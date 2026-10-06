namespace RWMS.Models.ViewModels.Product;

public class ProductSelectViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
}
