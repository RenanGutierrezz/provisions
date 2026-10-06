using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize(Policy = "OwnerAccess")]
public class FinanceController : Controller
{
    private readonly IFinanceService _finance;

    public FinanceController(IFinanceService finance)
    {
        _finance = finance;
    }

    [Authorize(Policy = "OwnerAccess")]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, CancellationToken ct)
    {
        return View(await _finance.GetReportAsync(from, to, ct));
    }
}
