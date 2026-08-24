using FSH.BlazorShared.Components;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class RolesListPage
{
    private static readonly string[] SystemRoleNames = ["Admin", "Basic"];

    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<RoleDto> _roles = [];
    private string _search = string.Empty;
    private bool _loading = true;
    private string? _error;

    private bool SearchActive => !string.IsNullOrWhiteSpace(_search);
    private int _totalCount => _roles.Count;

    private string MetaText => $"{_roles.Count} role{(_roles.Count == 1 ? string.Empty : "s")} found";

    private string EmptyStateDescription => SearchActive
        ? $"Nothing matches \"{_search.Trim()}\". Try a different term."
        : "Roles are the permission bundles you assign to users. Create the first one to start granting access.";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _roles = await RoleService.ListAsync();
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

    private IReadOnlyList<RoleDto> Filtered
    {
        get
        {
            if (!SearchActive)
            {
                return _roles;
            }

            var term = _search.Trim().ToLowerInvariant();
            return _roles.Where(r =>
                (r.Name ?? string.Empty).ToLowerInvariant().Contains(term)
                || (r.Description ?? string.Empty).ToLowerInvariant().Contains(term)).ToList();
        }
    }

    private void OpenDetail(RoleDto role) => Nav.NavigateTo($"/identity/roles/{role.Id}");

    private async Task OpenEditorAsync(RoleDto? role)
    {
        var parameters = new DialogParameters<RoleEditorDialog>
        {
            { x => x.Role, role },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<RoleEditorDialog>(role is null ? "New role" : "Edit role", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add(role is null ? "Role created" : "Role updated", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task ClearSearch()
    {
        _search = string.Empty;
        await LoadAsync();
    }

    private static bool IsSystemRole(RoleDto role)
        => role.Name is not null && SystemRoleNames.Contains(role.Name, StringComparer.OrdinalIgnoreCase);

    private static string Initial(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "?";
        }

        return char.ToUpperInvariant(name[0]).ToString();
    }
}
