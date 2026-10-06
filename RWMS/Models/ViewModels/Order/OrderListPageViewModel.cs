namespace RWMS.Models.ViewModels.Order;

public class OrderListPageViewModel
{
    public List<OrderListViewModel> Orders { get; set; } = [];
    public DateTime Date { get; set; }
    public string? Search { get; set; }
    public string? Filter { get; set; }

    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int ReadyCount { get; set; }
    public int RejectedCount { get; set; }
}
