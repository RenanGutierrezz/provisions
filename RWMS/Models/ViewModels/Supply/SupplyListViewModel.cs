namespace RWMS.Models.ViewModels.Supply;

public class SupplyListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
