using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize(Policy = "ManagerAccess")]
public class UserController : Controller
{
    private readonly IUserService _users;

    public UserController(IUserService users)
    {
        _users = users;
    }

    public async Task<IActionResult> Index(string? search, string? role, CancellationToken ct)
    {
        var model = await _users.GetAllUsersAsync(search, role, ct);
        return View(model);
    }

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        return View(await _users.GetUserDetailAsync(id, ct));
    }
}
