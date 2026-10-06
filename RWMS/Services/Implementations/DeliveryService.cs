using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.Enums;
using RWMS.Models.ViewModels.Delivery;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class DeliveryService : IDeliveryService
{
    private readonly ApplicationDbContext _db;

    public DeliveryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DeliveryListPageViewModel> GetAllDeliveriesAsync(CancellationToken ct = default)
    {
        var deliveries = await _db.Deliveries
            .AsNoTracking()
            .OrderByDescending(d => d.Date)
            .Select(d => new DeliveryListViewModel
            {
                Id = d.Id,
                Date = d.Date,
                DriverName = d.Driver != null ? d.Driver.FirstName + " " + d.Driver.LastName : "—",
                OrderCount = d.Orders.Count,
                IsComplete = d.IsComplete
            })
            .ToListAsync(ct);

        return new DeliveryListPageViewModel { Deliveries = deliveries };
    }

    public async Task<DeliveryDetailViewModel> GetDeliveryByIdAsync(int id, CancellationToken ct = default)
    {
        var delivery = await _db.Deliveries
            .AsNoTracking()
            .Include(d => d.Driver)
            .Include(d => d.Orders)
                .ThenInclude(o => o.Customer)
            .Include(d => d.Orders)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (delivery is null)
            throw new KeyNotFoundException($"Delivery {id} not found.");

        var availableOrders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Where(o => o.Status == OrderStatus.ReadyForDelivery && o.DeliveryId == null && o.IsActive)
            .OrderBy(o => o.Customer.CompanyName ?? o.Customer.FirstName)
            .Select(o => new OrderAssignViewModel
            {
                Id = o.Id,
                Label = "Order #" + o.Id + " — " + (o.Customer.CompanyName ?? (o.Customer.FirstName + " " + o.Customer.LastName))
            })
            .ToListAsync(ct);

        return new DeliveryDetailViewModel
        {
            Id = delivery.Id,
            Date = delivery.Date,
            DriverId = delivery.DriverId,
            DriverName = delivery.Driver?.FullName ?? "—",
            Notes = delivery.Notes,
            IsComplete = delivery.IsComplete,
            AvailableOrders = availableOrders,
            Stops = delivery.Orders
                .Where(o => o.IsActive)
                .OrderBy(o => o.Customer.DisplayName)
                .Select(o => new DeliveryStopViewModel
                {
                    OrderId = o.Id,
                    CustomerName = o.Customer.DisplayName,
                    CustomerAddress = o.Customer.Address,
                    TotalAmount = o.TotalAmount,
                    ItemSummaries = o.OrderItems
                        .Select(oi => $"{oi.Product.Name} × {oi.Quantity} {oi.Product.Unit}")
                        .ToList()
                })
                .ToList()
        };
    }

    public async Task<int> CreateDeliveryAsync(CreateDeliveryViewModel model, CancellationToken ct = default)
    {
        var delivery = new Delivery
        {
            Date = model.Date,
            DriverId = string.IsNullOrWhiteSpace(model.DriverId) ? null : model.DriverId,
            Notes = model.Notes
        };

        _db.Deliveries.Add(delivery);
        await _db.SaveChangesAsync(ct);

        return delivery.Id;
    }

    public async Task AssignOrderAsync(int deliveryId, int orderId, CancellationToken ct = default)
    {
        var delivery = await _db.Deliveries.FindAsync([deliveryId], ct);
        if (delivery is null)
            throw new KeyNotFoundException($"Delivery {deliveryId} not found.");

        var order = await _db.Orders.FindAsync([orderId], ct);
        if (order is null)
            throw new KeyNotFoundException($"Order {orderId} not found.");

        order.DeliveryId = deliveryId;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task UnassignOrderAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders.FindAsync([orderId], ct);
        if (order is null)
            throw new KeyNotFoundException($"Order {orderId} not found.");

        order.DeliveryId = null;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteDeliveryAsync(int id, CancellationToken ct = default)
    {
        var delivery = await _db.Deliveries.FindAsync([id], ct);
        if (delivery is null)
            throw new KeyNotFoundException($"Delivery {id} not found.");

        delivery.IsComplete = true;
        await _db.SaveChangesAsync(ct);
    }
}
