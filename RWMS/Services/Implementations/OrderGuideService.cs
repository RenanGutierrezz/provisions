using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class OrderGuideService : IOrderGuideService
{
    private readonly ApplicationDbContext _db;

    public OrderGuideService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<OrderGuideItem>> GetOrderGuideAsync(string customerId, CancellationToken ct = default)
    {
        var assignments = await _db.CustomerProducts
            .AsNoTracking()
            .Where(cp => cp.CustomerId == customerId && cp.Product.IsActive)
            .Select(cp => new OrderGuideItem
            {
                ProductId = cp.ProductId,
                Name = cp.Product.Name,
                Description = cp.Product.Description,
                Price = cp.Product.Price,
                Unit = cp.Product.Unit,
                Category = cp.Product.Category,
                ParLevel = cp.ParLevel
            })
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return assignments;

        var productIds = assignments.Select(a => a.ProductId).ToList();

        // Last order date per product for this customer
        var lastOrdered = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.CustomerId == customerId && productIds.Contains(oi.ProductId))
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, LastDate = g.Max(oi => oi.Order.CreatedAt) })
            .ToDictionaryAsync(x => x.ProductId, x => x.LastDate, ct);

        foreach (var item in assignments)
        {
            if (lastOrdered.TryGetValue(item.ProductId, out var date))
                item.LastOrderedAt = date;
        }

        return assignments;
    }

    public async Task<List<int>> GetAssignedProductIdsAsync(string customerId, CancellationToken ct = default)
    {
        return await _db.CustomerProducts
            .AsNoTracking()
            .Where(cp => cp.CustomerId == customerId)
            .Select(cp => cp.ProductId)
            .ToListAsync(ct);
    }

    public async Task SetAssignedProductsAsync(string customerId, List<int> productIds, CancellationToken ct = default)
    {
        var existing = await _db.CustomerProducts
            .Where(cp => cp.CustomerId == customerId)
            .ToListAsync(ct);

        var existingIds = existing.Select(cp => cp.ProductId).ToHashSet();
        var targetIds = productIds.ToHashSet();

        // Remove unselected
        var toRemove = existing.Where(cp => !targetIds.Contains(cp.ProductId)).ToList();
        _db.CustomerProducts.RemoveRange(toRemove);

        // Add newly selected
        var toAdd = targetIds.Except(existingIds).Select(pid => new CustomerProduct
        {
            CustomerId = customerId,
            ProductId = pid
        });
        _db.CustomerProducts.AddRange(toAdd);

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateParLevelAsync(string customerId, int productId, int? parLevel, CancellationToken ct = default)
    {
        var cp = await _db.CustomerProducts
            .FirstOrDefaultAsync(cp => cp.CustomerId == customerId && cp.ProductId == productId, ct);

        if (cp is null)
            throw new KeyNotFoundException($"Product {productId} is not assigned to customer {customerId}.");

        cp.ParLevel = parLevel;
        cp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
