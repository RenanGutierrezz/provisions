namespace RWMS.Models.Domain;

public class SupplyItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public decimal? UnitCost { get; set; }
    public bool IsOnList { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
