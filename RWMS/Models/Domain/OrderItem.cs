using RWMS.Models.Enums;

namespace RWMS.Models.Domain;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;

    // Captured at time of order — Product.Price may change later
    public decimal UnitPrice { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
