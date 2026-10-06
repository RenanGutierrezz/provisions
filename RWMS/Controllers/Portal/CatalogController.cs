using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RWMS.Models.Domain;
using RWMS.Models.ViewModels.Portal;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers.Portal;

[AllowAnonymous]
public class CatalogController : Controller
{
    private readonly IProductService _products;
    private readonly IOrderGuideService _orderGuide;
    private readonly UserManager<ApplicationUser> _userManager;

    public CatalogController(
        IProductService products,
        IOrderGuideService orderGuide,
        UserManager<ApplicationUser> userManager)
    {
        _products = products;
        _orderGuide = orderGuide;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? category, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);

        // Logged-in customer with assigned products → show their order guide
        if (user is not null && user.IsActive)
        {
            var guide = await _orderGuide.GetOrderGuideAsync(user.Id, ct);

            if (guide.Count > 0)
            {
                var products = guide.Select(g => new CatalogProductViewModel
                {
                    Id = g.ProductId,
                    Name = g.Name,
                    Description = g.Description,
                    Price = g.Price,
                    Unit = g.Unit,
                    Category = g.Category,
                    ParLevel = g.ParLevel,
                    LastOrderedAt = g.LastOrderedAt
                }).ToList();

                // Apply search/category filters client-side on the guide
                if (!string.IsNullOrWhiteSpace(search))
                    products = products.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!string.IsNullOrWhiteSpace(category))
                    products = products.Where(p => p.Category == category).ToList();

                var categories = guide
                    .Where(g => !string.IsNullOrWhiteSpace(g.Category))
                    .Select(g => g.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return View("~/Views/Portal/Catalog/Index.cshtml", new CatalogViewModel
                {
                    Search = search,
                    SelectedCategory = category,
                    Categories = categories,
                    Products = products
                });
            }
        }

        // Anonymous users or customers with no assignments → show all active products
        var result = await _products.GetAllProductsAsync(search, category, "active", ct);

        var vm = new CatalogViewModel
        {
            Search = search,
            SelectedCategory = category,
            Categories = result.Categories,
            Products = result.Products
                .Select(p => new CatalogProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Unit = p.Unit,
                    Category = p.Category
                })
                .ToList()
        };

        return View("~/Views/Portal/Catalog/Index.cshtml", vm);
    }


}
