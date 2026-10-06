using RWMS.Models.Enums;

namespace RWMS.Models.ViewModels.Order;

public class OrderDetailViewModel
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = [];
}
