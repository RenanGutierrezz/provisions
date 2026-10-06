using RWMS.Models.Enums;

namespace RWMS.Models.ViewModels.Order;

public class OrderItemViewModel
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;
    public OrderItemStatus Status { get; set; }
}
