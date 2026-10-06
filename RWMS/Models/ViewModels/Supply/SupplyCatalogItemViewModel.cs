namespace RWMS.Models.ViewModels.Supply;

public class SupplyCatalogItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int LastQuantity { get; set; }
}
