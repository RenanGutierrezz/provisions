using RWMS.Models.Domain;

namespace RWMS.Services.Interfaces;

public interface IOrderGuideService
{
    Task<List<OrderGuideItem>> GetOrderGuideAsync(string customerId, CancellationToken ct = default);
    Task<List<int>> GetAssignedProductIdsAsync(string customerId, CancellationToken ct = default);
    Task SetAssignedProductsAsync(string customerId, List<int> productIds, CancellationToken ct = default);
    Task UpdateParLevelAsync(string customerId, int productId, int? parLevel, CancellationToken ct = default);
}

public class OrderGuideItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? ParLevel { get; set; }
    public DateTime? LastOrderedAt { get; set; }
}
