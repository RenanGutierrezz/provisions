using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.ViewModels.Product;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;

    public ProductService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProductListPageViewModel> GetAllProductsAsync(string? search, string? category, string? status, CancellationToken ct = default)
    {
        var all = await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductListViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Unit = p.Unit,
                Category = p.Category,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        var categories = all
            .Where(p => !string.IsNullOrWhiteSpace(p.Category))
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(p => p.Category == category);

        if (status == "active")
            filtered = filtered.Where(p => p.IsActive);
        else if (status == "inactive")
            filtered = filtered.Where(p => !p.IsActive);

        return new ProductListPageViewModel
        {
            Products = filtered.ToList(),
            Search = search,
            Category = category,
            Status = status,
            Categories = categories
        };
    }

    public async Task<ProductDetailViewModel> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
            throw new KeyNotFoundException($"Product {id} not found.");

        return new ProductDetailViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Unit = product.Unit,
            Category = product.Category,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task CreateProductAsync(CreateProductViewModel model, CancellationToken ct = default)
    {
        var exists = await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Name == model.Name, ct);

        if (exists)
            throw new InvalidOperationException($"A product named '{model.Name}' already exists.");

        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            Unit = model.Unit,
            Category = model.Category
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateProductAsync(int id, EditProductViewModel model, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([id], ct);

        if (product is null)
            throw new KeyNotFoundException($"Product {id} not found.");

        var nameConflict = await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Name == model.Name && p.Id != id, ct);

        if (nameConflict)
            throw new InvalidOperationException($"A product named '{model.Name}' already exists.");

        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.Unit = model.Unit;
        product.Category = model.Category;
        product.IsActive = model.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateProductAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([id], ct);

        if (product is null)
            throw new KeyNotFoundException($"Product {id} not found.");

        product.IsActive = false;
        product.DeletedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ProductSelectViewModel>> GetActiveProductsForSelectAsync(CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProductSelectViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Unit = p.Unit
            })
            .ToListAsync(ct);
    }
}