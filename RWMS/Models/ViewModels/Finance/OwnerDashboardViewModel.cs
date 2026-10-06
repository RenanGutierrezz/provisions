using RWMS.Models.Enums;

namespace RWMS.Models.ViewModels.Finance;

public class OwnerDashboardViewModel
{
    public string UserFirstName { get; set; } = string.Empty;
    public int TotalCustomers { get; set; }
    public int PendingOrders { get; set; }
    public int AcceptedOrders { get; set; }
    public int OrdersReadyForDelivery { get; set; }
    public int PendingAccountRequests { get; set; }
    public int SupplyItemsOnList { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public List<MonthlyRevenueViewModel> MonthlyRevenue { get; set; } = [];
    public List<UpcomingDeliveryViewModel> UpcomingDeliveries { get; set; } = [];
}

public class MonthlyRevenueViewModel
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class UpcomingDeliveryViewModel
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime RequestedDeliveryDate { get; set; }
    public OrderStatus Status { get; set; }
}
