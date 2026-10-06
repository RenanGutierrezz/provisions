using RWMS.Models.ViewModels.Delivery;

namespace RWMS.Services.Interfaces;

public interface IDeliveryService
{
    Task<DeliveryListPageViewModel> GetAllDeliveriesAsync(CancellationToken ct = default);
    Task<DeliveryDetailViewModel> GetDeliveryByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateDeliveryAsync(CreateDeliveryViewModel model, CancellationToken ct = default);
    Task AssignOrderAsync(int deliveryId, int orderId, CancellationToken ct = default);
    Task UnassignOrderAsync(int orderId, CancellationToken ct = default);
    Task CompleteDeliveryAsync(int id, CancellationToken ct = default);
}
