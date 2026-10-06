using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.Enums;
using RWMS.Models.ViewModels.Finance;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class FinanceService : IFinanceService
{
    private readonly ApplicationDbContext _db;

    public FinanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<OwnerDashboardViewModel> GetDashboardAsync(CancellationToken ct = default)
    {
        var totalCustomers = await _db.Users.CountAsync(u => u.IsActive, ct);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.IsActive)
            .Include(o => o.Customer)
            .ToListAsync(ct);

        var ready = orders.Count(o => o.Status == OrderStatus.ReadyForDelivery);
        var accepted = orders.Count(o => o.Status == OrderStatus.Accepted);
        var pending = orders.Count(o => o.Status == OrderStatus.Pending);

        var pendingRequests = await _db.AccountRequests
            .CountAsync(r => r.Status == AccountRequestStatus.Pending, ct);

        var supplyOnList = await _db.SupplyItems.CountAsync(s => s.IsOnList, ct);
        var grossRevenue = orders
            .Where(o => o.Status != OrderStatus.Rejected)
            .Sum(o => o.TotalAmount);

        var monthly = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-5 + i);
                var revenue = orders
                    .Where(o => o.Status != OrderStatus.Rejected
                        && o.CreatedAt.Year == month.Year
                        && o.CreatedAt.Month == month.Month)
                    .Sum(o => o.TotalAmount);
                return new MonthlyRevenueViewModel
                {
                    Month = month.ToString("MMM"),
                    Revenue = revenue
                };
            }).ToList();

        var thisMonth = monthly.Last().Revenue;
        var lastMonth = monthly[^2].Revenue;
        var changePercent = lastMonth == 0 ? 0 : Math.Round((thisMonth - lastMonth) / lastMonth * 100, 1);

        var upcoming = orders
            .Where(o => o.RequestedDeliveryDate.HasValue
                && o.RequestedDeliveryDate.Value.Date >= DateTime.UtcNow.Date
                && o.Status != OrderStatus.Rejected)
            .OrderBy(o => o.RequestedDeliveryDate)
            .Take(5)
            .Select(o => new UpcomingDeliveryViewModel
            {
                OrderId = o.Id,
                CustomerName = o.Customer.DisplayName,
                RequestedDeliveryDate = o.RequestedDeliveryDate!.Value,
                Status = o.Status
            }).ToList();

        return new OwnerDashboardViewModel
        {
            TotalCustomers = totalCustomers,
            OrdersReadyForDelivery = ready,
            AcceptedOrders = accepted,
            PendingOrders = pending,
            PendingAccountRequests = pendingRequests,
            SupplyItemsOnList = supplyOnList,
            GrossRevenue = grossRevenue,
            RevenueChangePercent = changePercent,
            MonthlyRevenue = monthly,
            UpcomingDeliveries = upcoming
        };
    }

    public async Task<FinanceReportViewModel> GetReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var toEndOfDay = to.HasValue ? to.Value.Date.AddDays(1) : (DateTime?)null;

        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.IsActive && o.Status != OrderStatus.Rejected);

        if (from.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= from.Value.Date);

        if (toEndOfDay.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt < toEndOfDay.Value);

        var orders = await ordersQuery
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var orderLines = orders.Select(o => new OrderRevenueLineViewModel
        {
            OrderId = o.Id,
            CustomerName = o.Customer.DisplayName,
            OrderDate = o.CreatedAt,
            Amount = o.TotalAmount
        }).ToList();

        var byCustomer = orderLines
            .GroupBy(o => o.CustomerName)
            .Select(g => new CustomerRevenueViewModel
            {
                CustomerName = g.Key,
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(o => o.Amount)
            })
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();

        var top = byCustomer.FirstOrDefault();

        var chartMonths = new List<string>();
        var customerMonthlyRevenue = new List<CustomerMonthlyRevenueViewModel>();

        if (orderLines.Count > 0)
        {
            var earliest = orderLines.Min(o => o.OrderDate);
            var latest = orderLines.Max(o => o.OrderDate);
            var firstMonth = new DateTime(earliest.Year, earliest.Month, 1);
            var lastMonth = new DateTime(latest.Year, latest.Month, 1);

            var cursor = firstMonth;
            while (cursor <= lastMonth)
            {
                chartMonths.Add(cursor.ToString("MMM yyyy"));
                cursor = cursor.AddMonths(1);
            }

            var top5 = byCustomer.Take(5).Select(c => c.CustomerName).ToList();
            foreach (var name in top5)
            {
                var amounts = Enumerable.Range(0, chartMonths.Count).Select(i =>
                {
                    var month = firstMonth.AddMonths(i);
                    return orderLines
                        .Where(o => o.CustomerName == name
                            && o.OrderDate.Year == month.Year
                            && o.OrderDate.Month == month.Month)
                        .Sum(o => o.Amount);
                }).ToList();

                customerMonthlyRevenue.Add(new CustomerMonthlyRevenueViewModel
                {
                    CustomerName = name,
                    MonthlyAmounts = amounts
                });
            }
        }

        // Supply costs
        var supplyQuery = _db.SupplyItems
            .AsNoTracking()
            .Where(s => s.UnitCost.HasValue);

        if (from.HasValue)
            supplyQuery = supplyQuery.Where(s => s.CreatedAt >= from.Value.Date);

        if (toEndOfDay.HasValue)
            supplyQuery = supplyQuery.Where(s => s.CreatedAt < toEndOfDay.Value);

        var totalSupplyCost = await supplyQuery
            .SumAsync(s => s.QuantityNeeded * s.UnitCost!.Value, ct);

        var now = DateTime.UtcNow;
        var top5Base = (from == null && to == null)
            ? orderLines.Where(o => o.OrderDate.Year == now.Year && o.OrderDate.Month == now.Month)
            : orderLines;

        var topOrdersThisMonth = top5Base
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .ToList();

        return new FinanceReportViewModel
        {
            From = from,
            To = to,
            TotalRevenue = orderLines.Sum(o => o.Amount),
            TotalSupplyCost = totalSupplyCost,
            OrderCount = orderLines.Count,
            TopCustomerName = top?.CustomerName ?? string.Empty,
            TopCustomerRevenue = top?.TotalRevenue ?? 0,
            Orders = orderLines,
            RevenueByCustomer = byCustomer,
            ChartMonths = chartMonths,
            CustomerMonthlyRevenue = customerMonthlyRevenue,
            TopOrdersThisMonth = topOrdersThisMonth
        };
    }
}
