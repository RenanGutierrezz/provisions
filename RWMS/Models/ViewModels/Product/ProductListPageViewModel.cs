namespace RWMS.Models.ViewModels.Product;

public class ProductListPageViewModel
{
    public List<ProductListViewModel> Products { get; set; } = [];
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public List<string> Categories { get; set; } = [];
}
