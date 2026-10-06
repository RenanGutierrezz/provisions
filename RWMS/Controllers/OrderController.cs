using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using RWMS.Models.Domain;
using RWMS.Models.Enums;
using RWMS.Models.ViewModels.Order;
using RWMS.Services.Implementations;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize(Policy = "ManagerAccess")]
[AutoValidateAntiforgeryToken]
public class OrderController : Controller
{
    private readonly IOrderService _orders;
    private readonly IProductService _products;
    private readonly UserManager<ApplicationUser> _users;

    public OrderController(IOrderService orders, IProductService products, UserManager<ApplicationUser> users)
    {
        _orders = orders;
        _products = products;
        _users = users;
    }

    public async Task<IActionResult> Index(DateTime? date, string? search, string? filter, CancellationToken ct)
    {
        var selected = date ?? DateTime.Today;
        return View(await _orders.GetAllOrdersAsync(selected, search, filter, ct));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        return View(await _orders.GetOrderByIdAsync(id, ct));
    }

    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new CreateOrderViewModel
        {
            Customers = await GetClientSelectListAsync(),
            Products = await _products.GetActiveProductsForSelectAsync(ct)
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.Customers = await GetClientSelectListAsync();
            model.Products = await _products.GetActiveProductsForSelectAsync(ct);
            return View(model);
        }

        await _orders.CreateOrderAsync(model, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus, string? returnTo, CancellationToken ct)
    {
        await _orders.UpdateOrderStatusAsync(id, newStatus, ct);
        return returnTo == "index"
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateItemStatus(int orderId, int itemId, OrderItemStatus newStatus, CancellationToken ct)
    {
        await _orders.UpdateItemStatusAsync(orderId, itemId, newStatus, ct);
        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    public async Task<IActionResult> AcceptAllItems(int id, CancellationToken ct)
    {
        await _orders.AcceptAllItemsAsync(id, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RejectAllItems(int id, CancellationToken ct)
    {
        await _orders.RejectAllItemsAsync(id, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> DownloadInvoice(int id, CancellationToken ct)
    {
        var order = await _orders.GetOrderByIdAsync(id, ct);
        var bytes = new OrderInvoicePdf(order).GeneratePdf();
        return File(bytes, "application/pdf", $"invoice-order-{order.Id}.pdf");
    }

    public async Task<IActionResult> DownloadDaySheet(DateTime? date, CancellationToken ct)
    {
        var target = date ?? DateTime.Today;
        var orders = await _orders.GetOrdersByDateAsync(target, ct);
        var bytes = new OrdersDayPdf(orders, target).GeneratePdf();
        return File(bytes, "application/pdf", $"orders-{target:yyyy-MM-dd}.pdf");
    }

    private async Task<List<CustomerSelectViewModel>> GetClientSelectListAsync()
    {
        var clients = await _users.GetUsersInRoleAsync("Customer");
        return clients
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .Select(u => new CustomerSelectViewModel { Id = u.Id, Name = u.DisplayName })
            .ToList();
    }
}
