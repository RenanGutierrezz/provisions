using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RWMS.Models.Domain;
using RWMS.Models.ViewModels.Profile;

namespace RWMS.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public ProfileController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();

        var vm = new ProfilePageViewModel
        {
            Email = user.Email ?? string.Empty,
            Profile = new UpdateProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                EmailNotificationsEnabled = user.EmailNotificationsEnabled
            }
        };

        if (User.IsInRole("Customer"))
            return View("~/Views/Portal/Profile/Index.cshtml", vm);

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
        {
            var errorVm = new ProfilePageViewModel { Email = user.Email ?? string.Empty, Profile = model };
            return User.IsInRole("Customer")
                ? View("~/Views/Portal/Profile/Index.cshtml", errorVm)
                : View("Index", errorVm);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.EmailNotificationsEnabled = model.EmailNotificationsEnabled;

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _users.SetEmailAsync(user, model.Email);
            if (!emailResult.Succeeded)
            {
                foreach (var error in emailResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                var errorVm = new ProfilePageViewModel { Email = user.Email ?? string.Empty, Profile = model };
                return User.IsInRole("Customer")
                    ? View("~/Views/Portal/Profile/Index.cshtml", errorVm)
                    : View("Index", errorVm);
            }
            await _users.SetUserNameAsync(user, model.Email);
        }

        await _users.UpdateAsync(user);
        await _signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Profile updated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
        {
            return View("Index", new ProfilePageViewModel
            {
                Email = user.Email ?? string.Empty,
                Password = model
            });
        }

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            var errorVm = new ProfilePageViewModel { Email = user.Email ?? string.Empty, Password = model };
            return User.IsInRole("Customer")
                ? View("~/Views/Portal/Profile/Index.cshtml", errorVm)
                : View("Index", errorVm);
        }

        await _signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction(nameof(Index));
    }
}
