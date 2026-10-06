using System.ComponentModel.DataAnnotations;

namespace RWMS.Models.ViewModels.Finance;

public class FinanceReportViewModel
{
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public decimal TotalRevenue { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue => OrderCount == 0 ? 0 : TotalRevenue / OrderCount;
    public decimal TotalSupplyCost { get; set; }
    public decimal GrossProfit => TotalRevenue - TotalSupplyCost;
    public decimal ProfitMarginPercent => TotalRevenue == 0 ? 0 : Math.Round(GrossProfit / TotalRevenue * 100, 1);
    public string TopCustomerName { get; set; } = string.Empty;
    public decimal TopCustomerRevenue { get; set; }

    public List<OrderRevenueLineViewModel> Orders { get; set; } = [];
    public List<CustomerRevenueViewModel> RevenueByCustomer { get; set; } = [];
    public List<string> ChartMonths { get; set; } = [];
    public List<CustomerMonthlyRevenueViewModel> CustomerMonthlyRevenue { get; set; } = [];
    public List<OrderRevenueLineViewModel> TopOrdersThisMonth { get; set; } = [];
}
