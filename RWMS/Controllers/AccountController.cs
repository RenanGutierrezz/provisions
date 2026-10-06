using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RWMS.Models.Domain;
using RWMS.Models.ViewModels.Account;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IInviteService _invites;
    private readonly IEmailService _emailService;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IInviteService invites, IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _invites = invites;
        _emailService = emailService;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return await RedirectByRoleAsync(user);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked. Try again in 15 minutes.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    public async Task<IActionResult> Register(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        try
        {
            await _invites.ValidateAsync(token);
        }
        catch
        {
            TempData["Error"] = "This invitation is invalid or has expired.";
            return RedirectToAction(nameof(Login));
        }

        return View(new RegisterViewModel { Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(model.Token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(model);

        RWMS.Models.Domain.Invite invite;
        try
        {
            invite = await _invites.ValidateAsync(model.Token, ct);
        }
        catch
        {
            TempData["Error"] = "This invitation is invalid or has expired.";
            return RedirectToAction(nameof(Login));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, invite.Role);
            await _invites.RedeemAsync(model.Token, ct);
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Send customers to the portal; everyone else to the internal dashboard
            if (invite.Role == "Customer")
                return RedirectToAction("Index", "Catalog");

            return RedirectToAction("Owner", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            // Don't reveal whether an email/username is already registered
            if (error.Code is "DuplicateUserName" or "DuplicateEmail")
                ModelState.AddModelError(string.Empty, "Registration could not be completed. Please try again.");
            else
                ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        // Always show the same message — don't reveal whether the email exists
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action(nameof(ResetPassword), "Account",
                new { email = model.Email, token },
                protocol: Request.Scheme)!;

            await _emailService.SendPasswordResetAsync(user.Email!, user.FullName, resetUrl);
        }

        TempData["Info"] = "If that email is registered, a reset link has been sent.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    public IActionResult ResetPassword(string email, string token)
    {
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is null || !user.IsActive)
        {
            TempData["Error"] = "This reset link is invalid or has expired.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Info"] = "Password reset successfully. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> SearchUsers(string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(Array.Empty<object>());

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (u.FirstName.Contains(q) || u.LastName.Contains(q)))
            .OrderBy(u => u.FirstName)
            .Take(8)
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(ct);

        return Json(users);
    }

    private Task<IActionResult> RedirectByRoleAsync(ApplicationUser user)
    {
        return Task.FromResult<IActionResult>(RedirectToAction("Owner", "Dashboard"));
    }
}
