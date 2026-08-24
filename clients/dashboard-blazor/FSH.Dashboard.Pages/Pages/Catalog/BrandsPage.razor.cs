using FSH.BlazorShared.Components;
using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Catalog;

public sealed partial class BrandsPage
{
    [Inject] private ICatalogService Catalog { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private const int PageSize = 20;
    private List<BrandDto> _brands = new();
    private int _pageNumber = 1;
    private int _totalCount;
    private int _totalPages = 1;
    private string _search = string.Empty;

    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var page = await Catalog.SearchBrandsAsync(
                search: SearchActive ? _search.Trim() : null,
                pageNumber: _pageNumber,
                pageSize: PageSize,
                sortBy: "createdAtUtc",
                sortDir: "desc");
            _brands = page.Items.ToList();
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

    private IReadOnlyList<BrandDto> Filtered
    {
        get
        {
            if (!SearchActive)
            {
                return _brands;
            }

            var term = _search.Trim().ToLowerInvariant();
            return _brands.Where(b =>
                b.Name.ToLowerInvariant().Contains(term)
                || b.Slug.ToLowerInvariant().Contains(term)).ToList();
        }
    }

    private async Task GoToPage(int page)
    {
        _pageNumber = Math.Clamp(page, 1, _totalPages);
        await LoadAsync();
    }

    private async Task ClearSearch()
    {
        _search = string.Empty;
        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task OpenEditorAsync(BrandDto? brand)
    {
        var parameters = new DialogParameters { { "Brand", brand } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<BrandEditorDialog>(brand is null ? "Add a brand" : "Edit brand", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add(brand is null ? "Brand created" : "Brand updated", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task ConfirmDeleteAsync(BrandDto brand)
    {
        var parameters = new DialogParameters
        {
            { "Message", $"This permanently removes {brand.Name} (created {FshFormat.DateShort(brand.CreatedAtUtc)}). Products referencing this brand will need to be reassigned." },
            { "ConfirmText", "Delete brand" },
            { "CancelText", "Cancel" },
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Delete brand", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        try
        {
            await Catalog.DeleteBrandAsync(brand.Id);
            Snackbar.Add("Brand deleted", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private string EmptyStateDescription =>
        SearchActive
            ? $"Nothing matches \"{_search.Trim()}\". Try a different term or clear the search."
            : "Add your first brand to start building the catalog. Each brand carries its own slug, description, and logo.";

    private string MetaText =>
        SearchActive
            ? $"{Filtered.Count} brand{(Filtered.Count == 1 ? "" : "s")} matched on this page"
            : $"Showing {_brands.Count} of {_totalCount} brand{(_totalCount == 1 ? "" : "s")}"
              + (_totalPages > 1 ? $" · page {_pageNumber} of {_totalPages}" : "");

    private static string Initial(string name)
    {
        var trimmed = name.Trim();
        return string.IsNullOrEmpty(trimmed) ? "·" : trimmed[..1].ToUpperInvariant();
    }
}
