using FSH.BlazorShared.Components;
using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Catalog;

public sealed partial class CategoriesPage
{
    [Inject] private ICatalogService Catalog { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private const int PageSize = 50;
    private List<CategoryDto> _categories = new();
    private Dictionary<Guid, string> _parentNames = new();
    private int _pageNumber = 1;
    private int _totalCount;
    private int _totalPages = 1;
    private string _search = string.Empty;

    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await LoadParentNamesAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var page = await Catalog.SearchCategoriesAsync(
                search: SearchActive ? _search.Trim() : null,
                pageNumber: _pageNumber,
                pageSize: PageSize,
                sortBy: "name",
                sortDir: "asc");
            _categories = page.Items.ToList();
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

    private async Task LoadParentNamesAsync()
    {
        try
        {
            var tree = await Catalog.GetCategoryTreeAsync();
            _parentNames = new Dictionary<Guid, string>();
            void Walk(IEnumerable<CategoryTreeNodeDto> nodes)
            {
                foreach (var node in nodes)
                {
                    _parentNames[node.Id] = node.Name;
                    if (node.Children.Count > 0)
                    {
                        Walk(node.Children);
                    }
                }
            }

            Walk(tree);
        }
        catch
        {
            // parent names are a nicety; the list itself still renders
        }
    }

    private bool SearchActive => !string.IsNullOrWhiteSpace(_search);

    private IReadOnlyList<CategoryDto> Filtered
    {
        get
        {
            if (!SearchActive)
            {
                return _categories;
            }

            var term = _search.Trim().ToLowerInvariant();
            return _categories.Where(c =>
                c.Name.ToLowerInvariant().Contains(term)
                || c.Slug.ToLowerInvariant().Contains(term)).ToList();
        }
    }

    private string ParentName(Guid id) =>
        _parentNames.TryGetValue(id, out var name) ? name : "(parent)";

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

    private async Task OpenEditorAsync(CategoryDto? category)
    {
        var parameters = new DialogParameters { { "Category", category } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<CategoryEditorDialog>(string.Empty, parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add(category is null ? "Category created" : "Category updated", Severity.Success);
            await LoadParentNamesAsync();
            await LoadAsync();
        }
    }

    private async Task ConfirmDeleteAsync(CategoryDto category)
    {
        var parameters = new DialogParameters
        {
            { "Message", $"This permanently removes {category.Name} (created {FshFormat.DateShort(category.CreatedAtUtc)}). Categories with child categories cannot be deleted — move or delete the children first." },
            { "ConfirmText", "Delete category" },
            { "CancelText", "Cancel" },
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Delete category", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        try
        {
            await Catalog.DeleteCategoryAsync(category.Id);
            Snackbar.Add("Category deleted", Severity.Success);
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
            : "Categories give your catalog its tree. Create root shelves, then nest sub-shelves under them.";

    private string MetaText =>
        SearchActive
            ? $"{Filtered.Count} categor{(Filtered.Count == 1 ? "y" : "ies")} matched on this page"
            : $"Showing {_categories.Count} of {_totalCount} categor{(_totalCount == 1 ? "y" : "ies")}"
              + (_totalPages > 1 ? $" · page {_pageNumber} of {_totalPages}" : "");

    private static string Initial(string name)
    {
        var trimmed = name.Trim();
        return string.IsNullOrEmpty(trimmed) ? "·" : trimmed[..1].ToUpperInvariant();
    }
}
