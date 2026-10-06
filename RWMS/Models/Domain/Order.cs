using RWMS.Models.Enums;

namespace RWMS.Models.Domain;

public class Order
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int? DeliveryId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }

    // Stored so financial reports don't need to recalculate after product price changes
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Never hard-deleted — orders are permanent financial and history records
    public bool IsActive { get; set; } = true;

    public ApplicationUser Customer { get; set; } = null!;
    public Delivery? Delivery { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
