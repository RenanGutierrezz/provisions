using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RWMS.Models.ViewModels.Invite;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize(Policy = "OwnerAccess")]
public class InviteController : Controller
{
    private readonly IInviteService _invites;
    private readonly IEmailService _email;

    public InviteController(IInviteService invites, IEmailService email)
    {
        _invites = invites;
        _email = email;
    }

    public IActionResult SendInvite()
    {
        return View(new SendInviteViewModel());
    }

    // Roles an Owner is permitted to invite — never allow Owner-to-Owner escalation via form
    private static readonly HashSet<string> AllowedInviteRoles = ["Manager", "Customer"];

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvite(SendInviteViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!AllowedInviteRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Invalid role selected.");
            return View(model);
        }

        var token = await _invites.CreateInviteAsync(model.Email, model.Role, ct);

        var inviteUrl = Url.Action("Register", "Account", new { token }, Request.Scheme)!;
        await _email.SendInviteAsync(model.Email, model.Role, inviteUrl);

        TempData["Success"] = $"Invitation sent to {model.Email}.";
        return RedirectToAction(nameof(SendInvite));
    }
}
