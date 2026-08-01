using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Identity.Users;

public sealed partial class UserDetailPage
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private UserDto? _user;
    private List<UserRoleDto>? _roles;
    private List<UserSessionDto>? _sessions;
    private Dictionary<string, bool> _roleDraft = [];
    private Dictionary<string, bool> _roleOriginal = [];
    private string? _error;
    private bool _isToggling;
    private bool _isSavingRoles;

    private string DisplayName
    {
        get
        {
            var fullName = string.Join(' ', new[] { _user?.FirstName, _user?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            return !string.IsNullOrEmpty(fullName)
                ? fullName
                : _user?.UserName ?? _user?.Email ?? "User";
        }
    }

    private string Initials
    {
        get
        {
            var first = _user?.FirstName?.FirstOrDefault(c => char.IsLetter(c));
            var last = _user?.LastName?.FirstOrDefault(c => char.IsLetter(c));
            if (first is not null && last is not null)
            {
                return $"{first}{last}".ToUpperInvariant();
            }

            var name = _user?.UserName ?? _user?.Email ?? "?";
            return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
        }
    }

    private int _dirtyCount;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var userTask = UserService.GetAsync(Id);
            var rolesTask = UserService.GetRolesAsync(Id);
            var sessionsTask = UserService.GetSessionsAsync(Id);
            await Task.WhenAll(userTask, rolesTask, sessionsTask);

            _user = await userTask;
            _roles = await rolesTask;
            _sessions = await sessionsTask;
            _roleOriginal = _roles.ToDictionary(r => r.RoleId ?? r.RoleName ?? string.Empty, r => r.Enabled);
            _roleDraft = new Dictionary<string, bool>(_roleOriginal);
            _dirtyCount = 0;
        }
        catch (Exception ex)
        {
            _error = $"Failed to load user: {ex.Message}";
        }
    }

    private bool GetRoleEnabled(UserRoleDto role)
    {
        var key = role.RoleId ?? role.RoleName ?? string.Empty;
        return _roleDraft.TryGetValue(key, out var enabled) && enabled;
    }

    private async Task OnRoleToggledAsync(UserRoleDto role, bool value)
    {
        var key = role.RoleId ?? role.RoleName ?? string.Empty;
        _roleDraft[key] = value;
        RecalculateDirtyCount();
        await InvokeAsync(StateHasChanged);
    }

    private void RecalculateDirtyCount()
    {
        _dirtyCount = _roleDraft.Count(kvp =>
            _roleOriginal.TryGetValue(kvp.Key, out var original) && original != kvp.Value);
    }

    private async Task SaveRolesAsync()
    {
        _isSavingRoles = true;
        try
        {
            var updated = _roles!.Select(role =>
            {
                var key = role.RoleId ?? role.RoleName ?? string.Empty;
                return role with { Enabled = _roleDraft.TryGetValue(key, out var enabled) && enabled };
            }).ToList();

            await UserService.AssignRolesAsync(Id, updated);
            _roleOriginal = updated.ToDictionary(r => r.RoleId ?? r.RoleName ?? string.Empty, r => r.Enabled);
            _roleDraft = new Dictionary<string, bool>(_roleOriginal);
            _dirtyCount = 0;
            Snackbar.Add("Roles updated", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to update roles: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSavingRoles = false;
        }
    }

    private void DiscardRoles()
    {
        _roleDraft = new Dictionary<string, bool>(_roleOriginal);
        _dirtyCount = 0;
    }

    private async Task ToggleStatusAsync()
    {
        _isToggling = true;
        try
        {
            var activate = !_user!.IsActive;
            await UserService.ToggleStatusAsync(Id, activate);
            _user = _user with { IsActive = activate };
            Snackbar.Add(activate ? "User activated" : "User deactivated", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Status change failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isToggling = false;
        }
    }

    private void GoBackAsync() => Nav.NavigateTo("/users");
}
