using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.ViewModels.Order;
using RWMS.Models.ViewModels.Portal;
using RWMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RWMS.Controllers.Portal;

[Authorize(Policy = "CustomerAccess")]
public class CustomerOrderController : Controller
{
    private readonly IOrderService _orders;
    private readonly IProductService _products;
    private readonly IOrderGuideService _orderGuide;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerOrderController(
        IOrderService orders,
        IProductService products,
        IOrderGuideService orderGuide,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _orders = orders;
        _products = products;
        _orderGuide = orderGuide;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return View("~/Views/Portal/CustomerOrder/NoAccount.cshtml");

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == userId && o.IsActive)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var vm = orders.Select(o => new CustomerOrderSummaryViewModel
        {
            Id = o.Id,
            Status = o.Status.ToString(),
            RequestedDeliveryDate = o.RequestedDeliveryDate,
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            ItemCount = o.OrderItems.Sum(i => i.Quantity)
        }).ToList();

        return View("~/Views/Portal/CustomerOrder/Index.cshtml", vm);
    }

    public async Task<IActionResult> PlaceOrder(CancellationToken ct)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return View("~/Views/Portal/CustomerOrder/NoAccount.cshtml");

        var catalogProducts = await GetProductsForCustomerAsync(userId, ct);

        var vm = new PlaceOrderViewModel
        {
            RequestedDeliveryDate = DateTime.Today.AddDays(2),
            Products = catalogProducts,
            Items = [new PlaceOrderItemViewModel()]
        };

        return View("~/Views/Portal/CustomerOrder/PlaceOrder.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(PlaceOrderViewModel model, CancellationToken ct)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return View("~/Views/Portal/CustomerOrder/NoAccount.cshtml");

        model.Items = model.Items.Where(i => i.ProductId > 0).ToList();

        if (!ModelState.IsValid || model.Items.Count == 0)
        {
            model.Products = await GetProductsForCustomerAsync(userId, ct);

            if (model.Items.Count == 0)
                ModelState.AddModelError(string.Empty, "Add at least one item.");

            return View("~/Views/Portal/CustomerOrder/PlaceOrder.cshtml", model);
        }

        if (model.RequestedDeliveryDate < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.RequestedDeliveryDate), "Delivery date cannot be in the past.");
            model.Products = await GetProductsForCustomerAsync(userId, ct);
            return View("~/Views/Portal/CustomerOrder/PlaceOrder.cshtml", model);
        }

        await _orders.CreateOrderAsync(new CreateOrderViewModel
        {
            CustomerId = userId,
            RequestedDeliveryDate = model.RequestedDeliveryDate,
            Notes = model.Notes,
            Items = model.Items.Select(i => new CreateOrderItemViewModel
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        }, ct);

        TempData["Success"] = "Your order has been submitted. You will be contacted once it is confirmed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<CatalogProductViewModel>> GetProductsForCustomerAsync(string customerId, CancellationToken ct)
    {
        var guide = await _orderGuide.GetOrderGuideAsync(customerId, ct);

        if (guide.Count > 0)
        {
            return guide.Select(g => new CatalogProductViewModel
            {
                Id = g.ProductId,
                Name = g.Name,
                Price = g.Price,
                Unit = g.Unit,
                Category = g.Category,
                ParLevel = g.ParLevel,
                LastOrderedAt = g.LastOrderedAt
            }).ToList();
        }

        var all = await _products.GetActiveProductsForSelectAsync(ct);
        return all.Select(p => new CatalogProductViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Unit = p.Unit
        }).ToList();
    }
}
