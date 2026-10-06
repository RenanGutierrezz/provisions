namespace RWMS.Models.ViewModels.Finance;

public class CustomerRevenueViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
}
