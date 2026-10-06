using System.ComponentModel.DataAnnotations;

namespace RWMS.Models.ViewModels.Portal;

public class PlaceOrderViewModel
{
    [Required(ErrorMessage = "Please select a delivery date.")]
    public DateTime? RequestedDeliveryDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Add at least one item.")]
    public List<PlaceOrderItemViewModel> Items { get; set; } = [];

    // Populated for the view — not submitted by the user
    public List<CatalogProductViewModel> Products { get; set; } = [];
}

public class PlaceOrderItemViewModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a product.")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    public int Quantity { get; set; } = 1;
}
