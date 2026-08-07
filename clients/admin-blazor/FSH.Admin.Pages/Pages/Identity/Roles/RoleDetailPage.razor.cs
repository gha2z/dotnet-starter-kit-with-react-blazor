using System.ComponentModel.DataAnnotations;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Identity.Roles;

public sealed partial class RoleDetailPage
{
    public sealed class RoleProfileForm
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(64, MinimumLength = 2, ErrorMessage = "Name must be 2-64 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(256, ErrorMessage = "Keep under 256 characters.")]
        public string? Description { get; set; }
    }

    private sealed class PermissionGroup
    {
        public required string Resource { get; init; }
        public required List<PermissionCatalogEntryDto> Entries { get; init; }
        public int SelectedCount { get; set; }
        public bool AllSelected { get; set; }
        public bool AnySelected { get; set; }
    }

    private static readonly string[] SystemRoleNames = ["Admin", "Basic"];

    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;

    private MudForm? _profileForm;
    private RoleProfileForm _profileFormModel = new();
    private RoleDto? _role;
    private IReadOnlyList<PermissionCatalogEntryDto>? _catalog;
    private List<PermissionGroup>? _catalogGroups;
    private HashSet<string> _selected = [];
    private HashSet<string> _original = [];
    private string _deleteConfirm = string.Empty;
    private string? _error;
    private bool _isSavingProfile;
    private bool _isSavingPermissions;
    private bool _isDeleting;

    private bool IsSystemRole =>
        _role?.Name is not null && SystemRoleNames.Contains(_role.Name, StringComparer.OrdinalIgnoreCase);

    private bool _profileDirty =>
        _role is not null
        && (_profileFormModel.Name != (_role.Name ?? string.Empty)
            || (_profileFormModel.Description ?? string.Empty) != (_role.Description ?? string.Empty));

    private bool _permissionsDirty => !_selected.SetEquals(_original);

    private bool _deleteReady => _deleteConfirm == (_role?.Name ?? string.Empty);

    private int _totalPermissions => _catalogGroups?.Sum(g => g.Entries.Count) ?? 0;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var roleTask = RoleService.GetWithPermissionsAsync(Id);
            var catalogTask = RoleService.GetPermissionCatalogAsync();
            await Task.WhenAll(roleTask, catalogTask);

            _role = await roleTask;
            _catalog = await catalogTask;
            _selected = new HashSet<string>(_role.Permissions ?? [], StringComparer.Ordinal);
            _original = new HashSet<string>(_selected, StringComparer.Ordinal);
            ResetProfile();
            BuildGroups();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load role: {ex.Message}";
        }
    }

    private void ResetProfile()
    {
        _profileFormModel = new RoleProfileForm
        {
            Name = _role?.Name ?? string.Empty,
            Description = _role?.Description
        };
    }

    private async Task SaveProfileAsync()
    {
        if (_profileForm is null)
        {
            return;
        }

        await _profileForm.ValidateAsync();
        if (!_profileForm.IsValid)
        {
            return;
        }

        _isSavingProfile = true;
        try
        {
            var result = await RoleService.UpsertAsync(new UpsertRoleRequest(
                _role!.Id ?? string.Empty,
                _profileFormModel.Name,
                string.IsNullOrWhiteSpace(_profileFormModel.Description) ? null : _profileFormModel.Description));
            _role = result;
            ResetProfile();
            Snackbar.Add("Role updated", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to update role: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSavingProfile = false;
        }
    }

    private void BuildGroups()
    {
        _catalogGroups = [];
        foreach (var entry in _catalog ?? [])
        {
            var group = _catalogGroups.FirstOrDefault(g => g.Resource == entry.Resource);
            if (group is null)
            {
                group = new PermissionGroup { Resource = entry.Resource, Entries = [] };
                _catalogGroups.Add(group);
            }

            group.Entries.Add(entry);
        }

        RefreshGroupStates();
    }

    private void RefreshGroupStates()
    {
        foreach (var group in _catalogGroups ?? [])
        {
            var selectedCount = group.Entries.Count(e => _selected.Contains(e.Name));
            group.SelectedCount = selectedCount;
            group.AllSelected = selectedCount == group.Entries.Count;
            group.AnySelected = selectedCount > 0;
        }
    }

    private bool GetChecked(string permissionName) => _selected.Contains(permissionName);

    private void OnPermissionToggled(string permissionName, bool value)
    {
        if (value)
        {
            _selected.Add(permissionName);
        }
        else
        {
            _selected.Remove(permissionName);
        }

        RefreshGroupStates();
    }

    private void ToggleGroup(PermissionGroup group)
    {
        var turnOn = !group.AllSelected;
        foreach (var entry in group.Entries)
        {
            if (turnOn)
            {
                _selected.Add(entry.Name);
            }
            else
            {
                _selected.Remove(entry.Name);
            }
        }

        RefreshGroupStates();
    }

    private async Task SavePermissionsAsync()
    {
        _isSavingPermissions = true;
        try
        {
            await RoleService.UpdatePermissionsAsync(new UpdateRolePermissionsRequest(
                _role!.Id ?? string.Empty,
                [.. _selected.OrderBy(p => p, StringComparer.Ordinal)]));
            _original = new HashSet<string>(_selected, StringComparer.Ordinal);
            Snackbar.Add("Permissions updated", Severity.Success);

            // The current user's own permission claims are cached client-side; re-hydrate
            // them so gates and navigation reflect the change (React parity).
            if (AuthProvider is FSH.BlazorShared.Auth.AuthStateProvider authStateProvider)
            {
                await authStateProvider.RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to update permissions: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSavingPermissions = false;
        }
    }

    private void DiscardPermissions()
    {
        _selected = new HashSet<string>(_original, StringComparer.Ordinal);
        RefreshGroupStates();
    }

    private void OnDeleteConfirmChanged(string value)
    {
        _deleteConfirm = value;
    }

    private async Task DeleteRoleAsync()
    {
        _isDeleting = true;
        try
        {
            await RoleService.DeleteAsync(_role!.Id ?? string.Empty);
            Snackbar.Add($"Role {_role.Name} deleted", Severity.Success);
            Nav.NavigateTo("/roles");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to delete role: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private void GoBackAsync() => Nav.NavigateTo("/roles");
}
