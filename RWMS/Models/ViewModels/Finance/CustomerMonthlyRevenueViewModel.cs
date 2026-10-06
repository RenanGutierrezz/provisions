namespace RWMS.Models.ViewModels.Finance;

public class CustomerMonthlyRevenueViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public List<decimal> MonthlyAmounts { get; set; } = [];
}
