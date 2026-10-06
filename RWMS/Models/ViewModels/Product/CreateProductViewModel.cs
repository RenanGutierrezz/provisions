using System.ComponentModel.DataAnnotations;

namespace RWMS.Models.ViewModels.Product;

public class CreateProductViewModel
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Category { get; set; }
}