using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RWMS.Models.ViewModels.Product;
using RWMS.Services.Interfaces;

namespace RWMS.Controllers;

[Authorize]
public class ProductController : Controller
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> Index(string? search, string? category, string? status, CancellationToken ct)
    {
        return View(await _productService.GetAllProductsAsync(search, category, status, ct));
    }

    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var product = await _productService.GetProductByIdAsync(id, ct);
        return View(product);
    }

    [Authorize(Policy = "ManagerAccess")]
    public IActionResult Create()
    {
        return View(new CreateProductViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> Create(CreateProductViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _productService.CreateProductAsync(model, ct);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var product = await _productService.GetProductByIdAsync(id, ct);

        var model = new EditProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Unit = product.Unit,
            Category = product.Category,
            IsActive = product.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> Edit(int id, EditProductViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _productService.UpdateProductAsync(id, model, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ManagerAccess")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _productService.DeactivateProductAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
