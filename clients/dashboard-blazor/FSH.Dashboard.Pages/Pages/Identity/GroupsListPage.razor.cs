using FSH.BlazorShared.Components;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class GroupsListPage
{
    [Inject] private IGroupService GroupService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<GroupDto> _groups = [];
    private string _search = string.Empty;
    private bool _loading = true;
    private string? _error;

    private bool SearchActive => !string.IsNullOrWhiteSpace(_search);
    private int _totalCount => _groups.Count;

    private string MetaText => $"{_groups.Count} group{(_groups.Count == 1 ? string.Empty : "s")} found";

    private string EmptyStateDescription => SearchActive
        ? $"Nothing matches \"{_search.Trim()}\". Try a different term."
        : "Create the first group to bundle members and roles. Useful for teams, departments, or feature cohorts.";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _groups = await GroupService.ListAsync();
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

    private IReadOnlyList<GroupDto> Filtered
    {
        get
        {
            if (!SearchActive)
            {
                return _groups;
            }

            var term = _search.Trim().ToLowerInvariant();
            return _groups.Where(g =>
                (g.Name ?? string.Empty).ToLowerInvariant().Contains(term)
                || (g.Description ?? string.Empty).ToLowerInvariant().Contains(term)).ToList();
        }
    }

    private void OpenDetail(GroupDto group) => Nav.NavigateTo($"/identity/groups/{group.Id}");

    private async Task OpenCreateAsync()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<GroupEditorDialog>(null, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add("Group created", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task ClearSearch()
    {
        _search = string.Empty;
        await LoadAsync();
    }

    private static string Initial(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "?";
        }

        return char.ToUpperInvariant(name[0]).ToString();
    }
}
