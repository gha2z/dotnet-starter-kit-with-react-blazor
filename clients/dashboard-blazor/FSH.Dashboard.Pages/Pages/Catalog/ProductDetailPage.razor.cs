using FSH.BlazorShared.Components;
using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Catalog;

public sealed partial class ProductDetailPage
{
    [Inject] private ICatalogService Catalog { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private ProductDto? _product;
    private BrandDto? _brand;
    private CategoryDto? _category;
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _product = await Catalog.GetProductByIdAsync(Guid.Parse(Id));
            await LoadReferencesAsync();
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

    private async Task LoadReferencesAsync()
    {
        if (_product is null)
        {
            return;
        }

        try
        {
            var brandsTask = Catalog.SearchBrandsAsync(pageSize: 500, sortBy: "name", sortDir: "asc");
            var categoriesTask = Catalog.SearchCategoriesAsync(pageSize: 500, sortBy: "name", sortDir: "asc");
            await Task.WhenAll(brandsTask, categoriesTask);
            _brand = brandsTask.Result.Items.FirstOrDefault(b => b.Id == _product.BrandId);
            _category = categoriesTask.Result.Items.FirstOrDefault(c => c.Id == _product.CategoryId);
        }
        catch
        {
            // names are a nicety; the product still renders
        }
    }

    private async Task ReloadAsync() => await LoadAsync();

    private void GoBack() => Navigation.NavigateTo("/catalog/products");

    private async Task OpenPriceAsync()
    {
        if (_product is null)
        {
            return;
        }

        var parameters = new DialogParameters { { "Product", _product } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<PriceDialog>("Change price", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add("Price updated", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task OpenStockAsync()
    {
        if (_product is null)
        {
            return;
        }

        var parameters = new DialogParameters { { "Product", _product } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<StockDialog>("Adjust stock", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add("Stock adjusted", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task OpenEditorAsync()
    {
        if (_product is null)
        {
            return;
        }

        var brands = new List<BrandDto>();
        var categories = new List<CategoryDto>();
        try
        {
            var brandsTask = Catalog.SearchBrandsAsync(pageSize: 500, sortBy: "name", sortDir: "asc");
            var categoriesTask = Catalog.SearchCategoriesAsync(pageSize: 500, sortBy: "name", sortDir: "asc");
            await Task.WhenAll(brandsTask, categoriesTask);
            brands = brandsTask.Result.Items.ToList();
            categories = categoriesTask.Result.Items.ToList();
        }
        catch
        {
            // empty dropdowns fall back to create/edit disabled submit
        }

        var parameters = new DialogParameters
        {
            { "Product", _product },
            { "Brands", brands },
            { "Categories", categories },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<ProductEditorDialog>("Edit product", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add("Product updated", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task ConfirmDeleteAsync()
    {
        if (_product is null)
        {
            return;
        }

        var product = _product;
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
            Navigation.NavigateTo("/catalog/products");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task SetThumbnailAsync(ProductImageDto image)
    {
        if (_product is null)
        {
            return;
        }

        try
        {
            await Catalog.SetProductThumbnailAsync(_product.Id, image.Id);
            Snackbar.Add("Cover image updated", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Could not set cover: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteImageAsync(ProductImageDto image)
    {
        if (_product is null)
        {
            return;
        }

        try
        {
            await Catalog.DeleteProductImageAsync(_product.Id, image.Id);
            Snackbar.Add("Image removed", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Could not remove image: {ex.Message}", Severity.Error);
        }
    }

    private static RenderFragment StockChipInline(ProductDto product) => builder =>
    {
        var (cls, icon) = product.Stock switch
        {
            <= 0 => ("danger", Icons.Material.Filled.RemoveCircleOutline),
            <= 10 => ("warning", Icons.Material.Filled.WarningAmber),
            _ => ("default", Icons.Material.Filled.Inventory2),
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"fsh-stock-chip {cls} mt-1");
        builder.OpenComponent<MudIcon>(2);
        builder.AddAttribute(3, "Icon", icon);
        builder.AddAttribute(4, "Size", Size.Small);
        builder.CloseComponent();
        builder.AddContent(5, product.Stock <= 0 ? "out of stock" : product.Stock <= 10 ? $"low · {product.Stock} left" : "in stock");
        builder.CloseElement();
    };
}
