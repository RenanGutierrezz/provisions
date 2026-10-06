namespace RWMS.Models.ViewModels.Delivery;

public class DeliveryDetailViewModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsComplete { get; set; }
    public List<DeliveryStopViewModel> Stops { get; set; } = [];
    public List<OrderAssignViewModel> AvailableOrders { get; set; } = [];
}

public class OrderAssignViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class DeliveryStopViewModel
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public List<string> ItemSummaries { get; set; } = [];
    public decimal TotalAmount { get; set; }
}
