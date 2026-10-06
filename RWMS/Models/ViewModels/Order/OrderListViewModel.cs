using RWMS.Models.Enums;

namespace RWMS.Models.ViewModels.Order;

public class OrderListViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public List<string> ItemSummaries { get; set; } = [];
}
