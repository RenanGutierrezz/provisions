using System.ComponentModel.DataAnnotations;

namespace RWMS.Models.ViewModels.AccountRequest;

public class SubmitRequestViewModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number")]
    public string? Phone { get; set; }

    [StringLength(1000)]
    public string? Message { get; set; }
}
