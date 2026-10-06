using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RWMS.Models.Domain;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IFinanceService _finance;
    private readonly UserManager<ApplicationUser> _users;

    public DashboardController(IFinanceService finance, UserManager<ApplicationUser> users)
    {
        _finance = finance;
        _users = users;
    }

    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> Owner(CancellationToken ct)
    {
        var vm = await _finance.GetDashboardAsync(ct);
        var user = await _users.GetUserAsync(User);
        vm.UserFirstName = user?.FirstName ?? string.Empty;
        return View(vm);
    }
}
