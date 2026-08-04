using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Catalog;

namespace FSH.BlazorShared.Services;

public sealed class CatalogService(HttpClient http) : ICatalogService
{
    private const string CatalogBase = "/api/v1/catalog";

    private static string QueryString(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        return string.Join("&", parts);
    }

    private static PagedResult<T> EmptyPage<T>(int pageSize) =>
        new([], 1, pageSize, 0, 0, false, false);

    public async Task<PagedResult<BrandDto>> SearchBrandsAsync(
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default)
    {
        var query = QueryString(
            ("search", search),
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("sortBy", sortBy),
            ("sortDir", sortDir));
        return await http.GetFromJsonAsync<PagedResult<BrandDto>>($"{CatalogBase}/brands?{query}", ct)
            ?? EmptyPage<BrandDto>(pageSize);
    }

    public async Task<Guid> CreateBrandAsync(CreateBrandRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{CatalogBase}/brands", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> UpdateBrandAsync(UpdateBrandRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{CatalogBase}/brands/{request.BrandId}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task DeleteBrandAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{CatalogBase}/brands/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResult<CategoryDto>> SearchCategoriesAsync(
        string? search = null,
        Guid? parentCategoryId = null,
        int pageNumber = 1,
        int pageSize = 50,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default)
    {
        var query = QueryString(
            ("search", search),
            ("parentCategoryId", parentCategoryId?.ToString()),
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("sortBy", sortBy),
            ("sortDir", sortDir));
        return await http.GetFromJsonAsync<PagedResult<CategoryDto>>($"{CatalogBase}/categories?{query}", ct)
            ?? EmptyPage<CategoryDto>(pageSize);
    }

    public async Task<List<CategoryTreeNodeDto>> GetCategoryTreeAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<CategoryTreeNodeDto>>($"{CatalogBase}/categories/tree", ct) ?? [];
    }

    public async Task<Guid> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{CatalogBase}/categories", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{CatalogBase}/categories/{request.CategoryId}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{CatalogBase}/categories/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResult<ProductDto>> SearchProductsAsync(
        string? search = null,
        Guid? brandId = null,
        Guid? categoryId = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default)
    {
        var query = QueryString(
            ("search", search),
            ("brandId", brandId?.ToString()),
            ("categoryId", categoryId?.ToString()),
            ("isActive", isActive?.ToString().ToLowerInvariant()),
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("sortBy", sortBy),
            ("sortDir", sortDir));
        return await http.GetFromJsonAsync<PagedResult<ProductDto>>($"{CatalogBase}/products?{query}", ct)
            ?? EmptyPage<ProductDto>(pageSize);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<ProductDto>($"{CatalogBase}/products/{id}", ct)
            ?? throw new InvalidOperationException("Null product response");
    }

    public async Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{CatalogBase}/products", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> UpdateProductAsync(UpdateProductRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{CatalogBase}/products/{request.ProductId}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{CatalogBase}/products/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid> ChangeProductPriceAsync(Guid id, ChangeProductPriceRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"{CatalogBase}/products/{id}/price", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<int> AdjustProductStockAsync(Guid id, AdjustProductStockRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"{CatalogBase}/products/{id}/stock", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AdjustProductStockResponse>(ct);
        return result?.Stock ?? 0;
    }

    public async Task DeleteProductImageAsync(Guid productId, Guid imageId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{CatalogBase}/products/{productId}/images/{imageId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetProductThumbnailAsync(Guid productId, Guid imageId, CancellationToken ct = default)
    {
        var response = await http.PutAsync($"{CatalogBase}/products/{productId}/images/{imageId}/thumbnail", content: null, ct);
        response.EnsureSuccessStatusCode();
    }
}
