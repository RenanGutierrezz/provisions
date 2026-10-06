using System.ComponentModel.DataAnnotations;

namespace RWMS.Models.ViewModels.Delivery;

public class CreateDeliveryViewModel
{
    [Required(ErrorMessage = "Delivery date is required.")]
    public DateTime Date { get; set; } = DateTime.Today;

    public string? DriverId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
