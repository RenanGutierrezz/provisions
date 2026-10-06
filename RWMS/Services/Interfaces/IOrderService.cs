using RWMS.Models.Enums;
using RWMS.Models.ViewModels.Order;

namespace RWMS.Services.Interfaces;

public interface IOrderService
{
    Task<OrderListPageViewModel> GetAllOrdersAsync(DateTime date, string? search, string? filter, CancellationToken ct = default);
    Task<OrderDetailViewModel> GetOrderByIdAsync(int id, CancellationToken ct = default);
    Task UpdateOrderStatusAsync(int id, OrderStatus newStatus, CancellationToken ct = default);
    Task UpdateItemStatusAsync(int orderId, int itemId, OrderItemStatus newStatus, CancellationToken ct = default);
    Task AcceptAllItemsAsync(int orderId, CancellationToken ct = default);
    Task RejectAllItemsAsync(int orderId, CancellationToken ct = default);
    Task CreateOrderAsync(CreateOrderViewModel model, CancellationToken ct = default);
    Task<List<OrderDetailViewModel>> GetOrdersByDateAsync(DateTime date, CancellationToken ct = default);
}
