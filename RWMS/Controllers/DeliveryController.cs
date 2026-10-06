using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using RWMS.Models.Domain;
using RWMS.Models.ViewModels.Delivery;
using RWMS.Services.Implementations;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize(Policy = "ManagerAccess")]
[AutoValidateAntiforgeryToken]
public class DeliveryController : Controller
{
    private readonly IDeliveryService _deliveries;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailService _email;

    public DeliveryController(IDeliveryService deliveries, UserManager<ApplicationUser> users, IEmailService email)
    {
        _deliveries = deliveries;
        _users = users;
        _email = email;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _deliveries.GetAllDeliveriesAsync(ct));
    }

    public IActionResult Create() => View(new CreateDeliveryViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateDeliveryViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var id = await _deliveries.CreateDeliveryAsync(model, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        return View(await _deliveries.GetDeliveryByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<IActionResult> AssignOrder(int deliveryId, int orderId, CancellationToken ct)
    {
        await _deliveries.AssignOrderAsync(deliveryId, orderId, ct);
        return RedirectToAction(nameof(Details), new { id = deliveryId });
    }

    [HttpPost]
    public async Task<IActionResult> UnassignOrder(int deliveryId, int orderId, CancellationToken ct)
    {
        await _deliveries.UnassignOrderAsync(orderId, ct);
        return RedirectToAction(nameof(Details), new { id = deliveryId });
    }

    [HttpPost]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        await _deliveries.CompleteDeliveryAsync(id, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> NotifyDriver(int id, CancellationToken ct)
    {
        var delivery = await _deliveries.GetDeliveryByIdAsync(id, ct);

        if (delivery.DriverId == null)
            return RedirectToAction(nameof(Details), new { id });

        var driver = await _users.FindByIdAsync(delivery.DriverId);

        if (driver is null || !driver.EmailNotificationsEnabled || string.IsNullOrWhiteSpace(driver.Email))
            return RedirectToAction(nameof(Details), new { id });

        await _email.SendDeliveryNotificationAsync(driver.Email, driver.FullName, delivery);
        TempData["Success"] = "Delivery sheet sent to driver.";

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> DownloadSheet(int id, CancellationToken ct)
    {
        var delivery = await _deliveries.GetDeliveryByIdAsync(id, ct);
        var bytes = new DeliverySheetPdf(delivery).GeneratePdf();
        var driverSlug = string.IsNullOrWhiteSpace(delivery.DriverName) ? "unassigned" : delivery.DriverName.Replace(" ", "-");
        return File(bytes, "application/pdf", $"delivery-{delivery.Date:yyyy-MM-dd}-{driverSlug}.pdf");
    }
}
