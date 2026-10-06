using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RWMS.Models.ViewModels.AccountRequest;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

public class AccountRequestController : Controller
{
    private readonly IAccountRequestService _requests;
    private readonly IInviteService _invites;
    private readonly IEmailService _email;

    public AccountRequestController(IAccountRequestService requests, IInviteService invites, IEmailService email)
    {
        _requests = requests;
        _invites = invites;
        _email = email;
    }

    [AllowAnonymous]
    public IActionResult Apply()
    {
        return View(new SubmitRequestViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(SubmitRequestViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _requests.SubmitAsync(model, ct);
        return View("Submitted");
    }

    [Authorize(Policy = "OwnerAccess")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _requests.GetAllAsync(ct));
    }

    [HttpPost]
    [Authorize(Policy = "OwnerAccess")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = await _requests.ApproveAsync(id, reviewerId, ct);

        var token = await _invites.CreateInviteAsync(email, "Customer", ct);
        var inviteUrl = Url.Action("Register", "Account", new { token }, Request.Scheme)!;
        await _email.SendInviteAsync(email, "Customer", inviteUrl);

        TempData["Success"] = $"Request approved — invite sent to {email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "OwnerAccess")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _requests.RejectAsync(id, reviewerId, ct);

        TempData["Success"] = "Request rejected.";
        return RedirectToAction(nameof(Index));
    }
}
