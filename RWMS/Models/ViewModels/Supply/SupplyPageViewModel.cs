namespace RWMS.Models.ViewModels.Supply;

public class SupplyPageViewModel
{
    public List<SupplyListViewModel> Items { get; set; } = [];
    public string? Search { get; set; }
    public string? Category { get; set; }
    public List<string> Categories { get; set; } = [];
}
