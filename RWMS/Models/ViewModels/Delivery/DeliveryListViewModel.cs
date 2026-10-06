namespace RWMS.Models.ViewModels.Delivery;

public class DeliveryListPageViewModel
{
    public List<DeliveryListViewModel> Deliveries { get; set; } = [];
}

public class DeliveryListViewModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public bool IsComplete { get; set; }
}
