using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Identity.Roles;

public sealed partial class RolesListPage
{
    private static readonly string[] SystemRoleNames = ["Admin", "Basic"];

    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<RoleDto> _roles = [];
    private List<RoleDto> _filteredRoles = [];
    private string _searchInput = string.Empty;
    private string? _error;
    private CancellationTokenSource? _debounceCts;

    private bool _searchActive => !string.IsNullOrWhiteSpace(_searchInput);

    private string HeaderDescription => _roles.Count switch
    {
        0 => "Loading the role registry…",
        1 => "1 role on this tenant.",
        _ => $"{_roles.Count} roles on this tenant."
    };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var roles = await RoleService.ListAsync();
            _roles = roles
                .OrderBy(r => IsSystemRole(r) ? 0 : 1)
                .ThenBy(r => r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load roles: {ex.Message}";
        }
    }

    private async Task OnSearchChangedAsync(string value)
    {
        _searchInput = value;
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(250, _debounceCts.Token);
            ApplyFilter();
            await InvokeAsync(StateHasChanged);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void ApplyFilter()
    {
        var needle = _searchInput.Trim();
        _filteredRoles = string.IsNullOrEmpty(needle)
            ? [.. _roles]
            : _roles.Where(r =>
                (r.Name ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (r.Description ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task RowClickAsync(TableRowClickEventArgs<RoleDto> args)
    {
        if (args.Item?.Id is { } id)
        {
            Nav.NavigateTo($"/roles/{id}");
        }
    }

    private static string RowClassFunc(RoleDto role, int rowIndex) => "cursor-pointer";

    private async Task OpenCreateDialogAsync()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<RoleCreateDialog>("New role", options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is RoleDto role && role.Id is not null)
        {
            Snackbar.Add($"Role {role.Name} created", Severity.Success);
            Nav.NavigateTo($"/roles/{role.Id}");
        }
    }

    private static bool IsSystemRole(RoleDto role) =>
        role.Name is not null && SystemRoleNames.Contains(role.Name, StringComparer.OrdinalIgnoreCase);

    private static string PermissionCountLabel(RoleDto role) => role.Permissions switch
    {
        null => "—",
        { Count: 1 } => "1 permission",
        { Count: var n } => $"{n} permissions"
    };
}
