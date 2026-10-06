using RWMS.Models.ViewModels.Product;

namespace RWMS.Services.Interfaces;

public interface IProductService
{
    Task<ProductListPageViewModel> GetAllProductsAsync(string? search, string? category, string? status, CancellationToken ct = default);
    Task<ProductDetailViewModel> GetProductByIdAsync(int id, CancellationToken ct = default);
    Task CreateProductAsync(CreateProductViewModel model, CancellationToken ct = default);
    Task UpdateProductAsync(int id, EditProductViewModel model, CancellationToken ct = default);
    Task DeactivateProductAsync(int id, CancellationToken ct = default);
    Task<List<ProductSelectViewModel>> GetActiveProductsForSelectAsync(CancellationToken ct = default);
}