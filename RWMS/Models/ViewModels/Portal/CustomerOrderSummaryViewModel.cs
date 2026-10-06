namespace RWMS.Models.ViewModels.Portal;

public class CustomerOrderSummaryViewModel
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? RequestedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}
