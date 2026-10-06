using System.ComponentModel.DataAnnotations;

namespace RWMS.Models.ViewModels.Invite;

public class SendInviteViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = "Customer";
}
