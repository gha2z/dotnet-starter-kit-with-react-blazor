using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Catalog;

namespace FSH.BlazorShared.Services;

public interface ICatalogService
{
    // Brands
    Task<PagedResult<BrandDto>> SearchBrandsAsync(
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default);
    Task<Guid> CreateBrandAsync(CreateBrandRequest request, CancellationToken ct = default);
    Task<Guid> UpdateBrandAsync(UpdateBrandRequest request, CancellationToken ct = default);
    Task DeleteBrandAsync(Guid id, CancellationToken ct = default);

    // Categories
    Task<PagedResult<CategoryDto>> SearchCategoriesAsync(
        string? search = null,
        Guid? parentCategoryId = null,
        int pageNumber = 1,
        int pageSize = 50,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default);
    Task<List<CategoryTreeNodeDto>> GetCategoryTreeAsync(CancellationToken ct = default);
    Task<Guid> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<Guid> UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken ct = default);

    // Products
    Task<PagedResult<ProductDto>> SearchProductsAsync(
        string? search = null,
        Guid? brandId = null,
        Guid? categoryId = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default);
    Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Guid> UpdateProductAsync(UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteProductAsync(Guid id, CancellationToken ct = default);
    Task<Guid> ChangeProductPriceAsync(Guid id, ChangeProductPriceRequest request, CancellationToken ct = default);
    Task<int> AdjustProductStockAsync(Guid id, AdjustProductStockRequest request, CancellationToken ct = default);
    Task DeleteProductImageAsync(Guid productId, Guid imageId, CancellationToken ct = default);
    Task SetProductThumbnailAsync(Guid productId, Guid imageId, CancellationToken ct = default);
}
