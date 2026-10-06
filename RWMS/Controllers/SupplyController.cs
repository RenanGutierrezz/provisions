using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using RWMS.Models.ViewModels.Supply;
using RWMS.Services.Implementations;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize(Policy = "ManagerAccess")]
[AutoValidateAntiforgeryToken]
public class SupplyController : Controller
{
    private readonly ISupplyService _supply;

    public SupplyController(ISupplyService supply)
    {
        _supply = supply;
    }

    public async Task<IActionResult> Index(string? search, string? category, CancellationToken ct)
    {
        return View(await _supply.GetAllAsync(search, category, ct));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        return View(await _supply.GetByIdAsync(id, ct));
    }

    public IActionResult Create() => View(new CreateSupplyViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplyViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _supply.CreateAsync(model, ct);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var item = await _supply.GetByIdAsync(id, ct);
        return View(new EditSupplyViewModel
        {
            Name = item.Name,
            Category = item.Category,
            QuantityNeeded = item.QuantityNeeded,
            Unit = item.Unit,
            UnitCost = item.UnitCost,
            Notes = item.Notes
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EditSupplyViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _supply.UpdateAsync(id, model, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MarkComplete(int id, CancellationToken ct)
    {
        await _supply.MarkCompleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _supply.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ReAdd(int id, int quantity, CancellationToken ct)
    {
        await _supply.ReAddAsync(id, quantity, ct);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Catalog(CancellationToken ct)
    {
        return View(await _supply.GetCatalogAsync(ct));
    }

    public async Task<IActionResult> DownloadSheet(CancellationToken ct)
    {
        var page = await _supply.GetAllAsync(null, null, ct);
        var bytes = new SupplyOrderPdf(page.Items, DateTime.UtcNow).GeneratePdf();
        return File(bytes, "application/pdf", $"shopping-list-{DateTime.UtcNow:yyyy-MM-dd}.pdf");
    }
}
