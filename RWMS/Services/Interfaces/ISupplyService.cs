using RWMS.Models.ViewModels.Supply;

namespace RWMS.Services.Interfaces;

public interface ISupplyService
{
    Task<SupplyPageViewModel> GetAllAsync(string? search, string? category, CancellationToken ct = default);
    Task<SupplyDetailViewModel> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(CreateSupplyViewModel model, CancellationToken ct = default);
    Task UpdateAsync(int id, EditSupplyViewModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task MarkCompleteAsync(int id, CancellationToken ct = default);
    Task<List<SupplyCatalogItemViewModel>> GetCatalogAsync(CancellationToken ct = default);
    Task ReAddAsync(int id, int quantity, CancellationToken ct = default);
}
