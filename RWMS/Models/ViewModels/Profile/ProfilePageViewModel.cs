namespace RWMS.Models.ViewModels.Profile;

public class ProfilePageViewModel
{
    public string Email { get; set; } = string.Empty;
    public UpdateProfileViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel Password { get; set; } = new();
}
