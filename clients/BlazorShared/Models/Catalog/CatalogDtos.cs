namespace FSH.BlazorShared.Models.Catalog;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);

public sealed record CategoryTreeNodeDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyList<CategoryTreeNodeDto> Children);

public sealed record MoneyDto(decimal Amount, string Currency);

public sealed record ProductImageDto(
    Guid Id,
    Guid? FileAssetId,
    string Url,
    bool IsThumbnail,
    int SortOrder,
    DateTime CreatedAtUtc);

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    MoneyDto Price,
    int Stock,
    bool IsActive,
    string? ThumbnailUrl,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);

// ---- request DTOs ----

public sealed record CreateBrandRequest(string Name, string? Description, string? LogoUrl);
public sealed record UpdateBrandRequest(Guid BrandId, string Name, string? Description, string? LogoUrl);
public sealed record CreateCategoryRequest(string Name, string? Description, Guid? ParentCategoryId);
public sealed record UpdateCategoryRequest(Guid CategoryId, string Name, string? Description, Guid? ParentCategoryId);
public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    decimal PriceAmount,
    string PriceCurrency,
    int Stock);
public sealed record UpdateProductRequest(
    Guid ProductId,
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    bool IsActive);
public sealed record ChangeProductPriceRequest(decimal Amount, string Currency);
public sealed record AdjustProductStockRequest(int Delta);
public sealed record AdjustProductStockResponse(int Stock);
public sealed record AddProductImageRequest(Guid? FileAssetId, string Url);
