using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.Enums;
using RWMS.Models.ViewModels.Order;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;

    public OrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<OrderListPageViewModel> GetAllOrdersAsync(DateTime date, string? search, string? filter, CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.IsActive && o.RequestedDeliveryDate.HasValue && o.RequestedDeliveryDate.Value.Date == date.Date)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListViewModel
            {
                Id = o.Id,
                CustomerName = o.Customer.CompanyName ?? (o.Customer.FirstName + " " + o.Customer.LastName),
                Status = o.Status,
                RequestedDeliveryDate = o.RequestedDeliveryDate,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                ItemCount = o.OrderItems.Count,
                ItemSummaries = o.OrderItems.Select(oi => oi.Product.Name + " (x" + oi.Quantity + ")").ToList()
            })
            .ToListAsync(ct);

        var totalCount = orders.Count;
        var pendingCount = orders.Count(o => o.Status == OrderStatus.Pending);
        var acceptedCount = orders.Count(o => o.Status == OrderStatus.Accepted);
        var readyCount = orders.Count(o => o.Status == OrderStatus.ReadyForDelivery);
        var rejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected);

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (int.TryParse(search.Trim(), out var orderId))
                orders = orders.Where(o => o.Id == orderId).ToList();
            else
                orders = orders.Where(o => o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter) && Enum.TryParse<OrderStatus>(filter, out var status))
            orders = orders.Where(o => o.Status == status).ToList();

        return new OrderListPageViewModel
        {
            Orders = orders,
            Date = date.Date,
            Search = search,
            Filter = filter,
            TotalCount = totalCount,
            PendingCount = pendingCount,
            AcceptedCount = acceptedCount,
            ReadyCount = readyCount,
            RejectedCount = rejectedCount
        };
    }

    public async Task<OrderDetailViewModel> GetOrderByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw new KeyNotFoundException($"Order {id} not found.");

        return new OrderDetailViewModel
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer.DisplayName,
            CustomerEmail = order.Customer.Email ?? string.Empty,
            CustomerAddress = order.Customer.Address,
            Status = order.Status,
            RequestedDeliveryDate = order.RequestedDeliveryDate,
            Notes = order.Notes,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemViewModel
            {
                Id = oi.Id,
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Status = oi.Status
            }).ToList()
        };
    }

    public async Task UpdateOrderStatusAsync(int id, OrderStatus newStatus, CancellationToken ct = default)
    {
        var order = await _db.Orders.FindAsync([id], ct);

        if (order is null)
            throw new KeyNotFoundException($"Order {id} not found.");

        if (order.Status == newStatus)
            return;

        var valid = (order.Status, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Accepted) => true,
            (OrderStatus.Pending, OrderStatus.Rejected) => true,
            (OrderStatus.Accepted, OrderStatus.ReadyForDelivery) => true,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException($"Cannot transition order from {order.Status} to {newStatus}.");

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // When accepting/rejecting the whole order, set all pending items to match
        if (newStatus == OrderStatus.Accepted)
        {
            var items = await _db.OrderItems.Where(oi => oi.OrderId == id).ToListAsync(ct);
            foreach (var item in items.Where(i => i.Status == OrderItemStatus.Pending))
                item.Status = OrderItemStatus.Accepted;
        }
        else if (newStatus == OrderStatus.Rejected)
        {
            var items = await _db.OrderItems.Where(oi => oi.OrderId == id).ToListAsync(ct);
            foreach (var item in items.Where(i => i.Status == OrderItemStatus.Pending))
                item.Status = OrderItemStatus.Rejected;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateItemStatusAsync(int orderId, int itemId, OrderItemStatus newStatus, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Items can only be accepted/rejected while the order is Pending.");

        var item = order.OrderItems.FirstOrDefault(oi => oi.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in order {orderId}.");

        item.Status = newStatus;

        // If no items are still Pending, auto-resolve the order
        if (order.OrderItems.All(oi => oi.Status != OrderItemStatus.Pending))
        {
            var hasAccepted = order.OrderItems.Any(oi => oi.Status == OrderItemStatus.Accepted);
            order.Status = hasAccepted ? OrderStatus.Accepted : OrderStatus.Rejected;
            order.TotalAmount = order.OrderItems
                .Where(oi => oi.Status == OrderItemStatus.Accepted)
                .Sum(oi => oi.Quantity * oi.UnitPrice);
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AcceptAllItemsAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Items can only be accepted while the order is Pending.");

        foreach (var item in order.OrderItems)
            item.Status = OrderItemStatus.Accepted;

        order.Status = OrderStatus.Accepted;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RejectAllItemsAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Items can only be rejected while the order is Pending.");

        foreach (var item in order.OrderItems)
            item.Status = OrderItemStatus.Rejected;

        order.Status = OrderStatus.Rejected;
        order.TotalAmount = 0;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CreateOrderAsync(CreateOrderViewModel model, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([model.CustomerId], ct);
        if (user is null)
            throw new KeyNotFoundException($"User {model.CustomerId} not found.");

        var productIds = model.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync(ct);

        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more selected products are unavailable.");

        var items = model.Items.Select(i =>
        {
            var product = products.First(p => p.Id == i.ProductId);
            return new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        var total = items.Sum(i => i.Quantity * i.UnitPrice);

        var order = new Order
        {
            CustomerId = model.CustomerId,
            RequestedDeliveryDate = model.RequestedDeliveryDate,
            Notes = model.Notes,
            TotalAmount = total,
            OrderItems = items
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<OrderDetailViewModel>> GetOrdersByDateAsync(DateTime date, CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.IsActive && o.CreatedAt.Date == date.Date)
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderBy(o => o.Customer.CompanyName ?? o.Customer.FirstName)
            .ToListAsync(ct);

        return orders.Select(o => new OrderDetailViewModel
        {
            Id = o.Id,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer.DisplayName,
            CustomerEmail = o.Customer.Email ?? string.Empty,
            Status = o.Status,
            RequestedDeliveryDate = o.RequestedDeliveryDate,
            Notes = o.Notes,
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            Items = o.OrderItems.Select(oi => new OrderItemViewModel
            {
                Id = oi.Id,
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Status = oi.Status
            }).ToList()
        }).ToList();
    }
}
