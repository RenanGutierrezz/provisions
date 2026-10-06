using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.ViewModels.Supply;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class SupplyService : ISupplyService
{
    private readonly ApplicationDbContext _db;

    public SupplyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SupplyPageViewModel> GetAllAsync(string? search, string? category, CancellationToken ct = default)
    {
        var items = await _db.SupplyItems
            .AsNoTracking()
            .Where(s => s.IsOnList)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new SupplyListViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Notes = s.Notes,
                QuantityNeeded = s.QuantityNeeded,
                Unit = s.Unit,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(ct);

        var categories = items
            .Select(i => string.IsNullOrWhiteSpace(i.Category) ? "Other" : i.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            items = items.Where(i => i.Name.ToLower().Contains(term)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            items = category == "Other"
                ? items.Where(i => string.IsNullOrWhiteSpace(i.Category)).ToList()
                : items.Where(i => i.Category == category).ToList();
        }

        return new SupplyPageViewModel
        {
            Items = items,
            Search = search,
            Category = category,
            Categories = categories
        };
    }

    public async Task<SupplyDetailViewModel> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.SupplyItems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (item is null)
            throw new KeyNotFoundException($"Supply item {id} not found.");

        return new SupplyDetailViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            QuantityNeeded = item.QuantityNeeded,
            Unit = item.Unit,
            Notes = item.Notes,
            UnitCost = item.UnitCost,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public async Task CreateAsync(CreateSupplyViewModel model, CancellationToken ct = default)
    {
        var item = new SupplyItem
        {
            Name = model.Name,
            Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category.Trim(),
            QuantityNeeded = model.QuantityNeeded,
            Unit = model.Unit,
            UnitCost = model.UnitCost,
            Notes = model.Notes
        };

        _db.SupplyItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(int id, EditSupplyViewModel model, CancellationToken ct = default)
    {
        var item = await _db.SupplyItems.FindAsync([id], ct);

        if (item is null)
            throw new KeyNotFoundException($"Supply item {id} not found.");

        item.Name = model.Name;
        item.Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category.Trim();
        item.QuantityNeeded = model.QuantityNeeded;
        item.Unit = model.Unit;
        item.UnitCost = model.UnitCost;
        item.Notes = model.Notes;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.SupplyItems.FindAsync([id], ct);

        if (item is null)
            throw new KeyNotFoundException($"Supply item {id} not found.");

        _db.SupplyItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkCompleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.SupplyItems.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Supply item {id} not found.");

        item.IsOnList = false;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<SupplyCatalogItemViewModel>> GetCatalogAsync(CancellationToken ct = default)
    {
        return await _db.SupplyItems
            .AsNoTracking()
            .Where(s => !s.IsOnList)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new SupplyCatalogItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Unit = s.Unit,
                Category = s.Category,
                LastQuantity = s.QuantityNeeded
            })
            .ToListAsync(ct);
    }

    public async Task ReAddAsync(int id, int quantity, CancellationToken ct = default)
    {
        var item = await _db.SupplyItems.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Supply item {id} not found.");

        item.IsOnList = true;
        item.QuantityNeeded = quantity > 0 ? quantity : item.QuantityNeeded;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
