using System.ComponentModel.DataAnnotations;

namespace RWMS.Models.ViewModels.Supply;

public class CreateSupplyViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quantity needed is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int QuantityNeeded { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;

    [Range(0.01, 999999.99, ErrorMessage = "Cost must be a positive number")]
    public decimal? UnitCost { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
