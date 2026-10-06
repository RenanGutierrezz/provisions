namespace RWMS.Models.ViewModels.Finance;

public class OrderRevenueLineViewModel
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Amount { get; set; }
}
