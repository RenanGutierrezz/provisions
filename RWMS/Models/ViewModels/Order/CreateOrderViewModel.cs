using System.ComponentModel.DataAnnotations;
using RWMS.Models.ViewModels.Product;

namespace RWMS.Models.ViewModels.Order;

public class CreateOrderViewModel
{
    [Required(ErrorMessage = "Please select a customer.")]
    public string CustomerId { get; set; } = string.Empty;

    public DateTime? RequestedDeliveryDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<CreateOrderItemViewModel> Items { get; set; } = [new()];

    // Populated for the form dropdowns
    public List<CustomerSelectViewModel> Customers { get; set; } = [];
    public List<ProductSelectViewModel> Products { get; set; } = [];
}

public class CreateOrderItemViewModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a product.")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    public int Quantity { get; set; } = 1;
}

public class CustomerSelectViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
