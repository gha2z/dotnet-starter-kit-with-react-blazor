using FSH.BlazorShared.Components;
using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Catalog;

public sealed partial class ProductsPage
{
    [Inject] private ICatalogService Catalog { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private const int PageSize = 25;
    private List<ProductDto> _products = new();
    private List<BrandDto> _brands = new();
    private List<CategoryDto> _categories = new();
    private int _pageNumber = 1;
    private int _totalCount;
    private int _totalPages = 1;
    private string _search = string.Empty;
    private Guid? _brandFilter;
    private Guid? _categoryFilter;
    private bool? _visibilityFilter;

    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await LoadReferenceDataAsync();
        await LoadAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            var brandsTask = Catalog.SearchBrandsAsync(pageSize: 500, sortBy: "name", sortDir: "asc");
            var categoriesTask = Catalog.SearchCategoriesAsync(pageSize: 500, sortBy: "name", sortDir: "asc");
            await Task.WhenAll(brandsTask, categoriesTask);
            _brands = brandsTask.Result.Items.ToList();
            _categories = categoriesTask.Result.Items.ToList();
        }
        catch
        {
            // filter dropdowns are a nicety; the list still renders
        }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var page = await Catalog.SearchProductsAsync(
                search: SearchActive ? _search.Trim() : null,
                brandId: _brandFilter,
                categoryId: _categoryFilter,
                isActive: _visibilityFilter,
                pageNumber: _pageNumber,
                pageSize: PageSize,
                sortBy: "createdAtUtc",
                sortDir: "desc");
            _products = page.Items.ToList();
            _totalCount = page.TotalCount;
            _totalPages = page.TotalPages;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private bool SearchActive => !string.IsNullOrWhiteSpace(_search);
    private bool FilterActive => SearchActive || _brandFilter.HasValue || _categoryFilter.HasValue || _visibilityFilter.HasValue;

    private IReadOnlyList<ProductDto> Filtered
    {
        get
        {
            if (!SearchActive)
            {
                return _products;
            }

            var term = _search.Trim().ToLowerInvariant();
            return _products.Where(p =>
                p.Name.ToLowerInvariant().Contains(term)
                || p.Sku.ToLowerInvariant().Contains(term)
                || p.Slug.ToLowerInvariant().Contains(term)).ToList();
        }
    }

    private void SetVisibility(bool? value)
    {
        _visibilityFilter = value;
        _pageNumber = 1;
        _ = LoadAsync();
    }

    private void GoToPage(int page)
    {
        _pageNumber = Math.Clamp(page, 1, _totalPages);
        _ = LoadAsync();
    }

    private void ClearFilters()
    {
        _search = string.Empty;
        _brandFilter = null;
        _categoryFilter = null;
        _visibilityFilter = null;
        _pageNumber = 1;
        _ = LoadAsync();
    }

    private string BrandName(Guid id) => _brands.FirstOrDefault(b => b.Id == id)?.Name ?? "—";
    private string CategoryName(Guid id) => _categories.FirstOrDefault(c => c.Id == id)?.Name ?? "—";

    private static RenderFragment StockChip(ProductDto product) => builder =>
    {
        var (cls, icon) = product.Stock switch
        {
            <= 0 => ("danger", Icons.Material.Filled.RemoveCircleOutline),
            <= 10 => ("warning", Icons.Material.Filled.WarningAmber),
            _ => ("default", Icons.Material.Filled.Inventory2),
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"fsh-stock-chip {cls}");
        builder.OpenComponent<MudIcon>(2);
        builder.AddAttribute(3, "Icon", icon);
        builder.AddAttribute(4, "Size", Size.Small);
        builder.CloseComponent();
        builder.AddContent(5, product.Stock <= 0 ? "out" : product.Stock.ToString());
        builder.CloseElement();
    };

    private async Task OpenEditorAsync(ProductDto? product)
    {
        var parameters = new DialogParameters
        {
            { "Product", product },
            { "Brands", _brands },
            { "Categories", _categories },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<ProductEditorDialog>(product is null ? "Add a product" : "Edit product", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add(product is null ? "Product created" : "Product updated", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task ConfirmDeleteAsync(ProductDto product)
    {
        var parameters = new DialogParameters
        {
            { "Message", $"This permanently removes {product.Name} (created {FshFormat.DateShort(product.CreatedAtUtc)}). The product will no longer appear in any listing or report." },
            { "ConfirmText", "Delete product" },
            { "CancelText", "Cancel" },
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Delete product", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        try
        {
            await Catalog.DeleteProductAsync(product.Id);
            Snackbar.Add("Product deleted", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private void OpenDetail(ProductDto product) => Navigation.NavigateTo($"/catalog/products/{product.Id}");

    private string EmptyStateDescription =>
        FilterActive
            ? $"Nothing matches the current search and filters. Try adjusting the criteria above."
            : "Add your first product to start building the catalog. Give it a brand, a category, a price, and a starting stock.";

    private string MetaText =>
        FilterActive
            ? $"{Filtered.Count} product{(Filtered.Count == 1 ? "" : "s")} matched on this page"
            : $"Showing {_products.Count} of {_totalCount} product{(_totalCount == 1 ? "" : "s")}"
              + (_totalPages > 1 ? $" · page {_pageNumber} of {_totalPages}" : "");

    private static string Initial(string name)
    {
        var trimmed = name.Trim();
        return string.IsNullOrEmpty(trimmed) ? "·" : trimmed[..1].ToUpperInvariant();
    }
}
