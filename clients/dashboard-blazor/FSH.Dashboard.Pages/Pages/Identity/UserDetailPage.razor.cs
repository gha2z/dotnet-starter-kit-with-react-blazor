using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Components;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class UserDetailPage
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IImpersonationService ImpersonationService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AuthStateProvider AuthState { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;

    private UserDto? _user;
    private List<UserRoleDto>? _roles;
    private List<UserSessionDto>? _sessions;
    private Dictionary<string, bool> _roleDraft = [];
    private Dictionary<string, bool> _roleOriginal = [];
    private List<Guid> _isRevokingSessions = [];

    private bool _loading = true;
    private bool _isToggling;
    private bool _isSavingRoles;
    private bool _isRevokingAll;
    private string? _error;

    private int _dirtyCount;

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

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var userTask = UserService.GetAsync(Id);
            var rolesTask = UserService.GetRolesAsync(Id);
            var sessionsTask = UserService.GetSessionsAsync(Id);
            await Task.WhenAll(userTask, rolesTask, sessionsTask);

            _user = userTask.Result;
            _roles = rolesTask.Result;
            _sessions = sessionsTask.Result;
            _roleOriginal = _roles.ToDictionary(r => r.RoleId ?? r.RoleName ?? string.Empty, r => r.Enabled);
            _roleDraft = new Dictionary<string, bool>(_roleOriginal);
            _dirtyCount = 0;
        }
        catch (Exception ex)
        {
            _error = $"Failed to load user: {ex.Message}";
        }
        finally
        {
            _loading = false;
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
            Snackbar.Add($"{updated.Count(r => r.Enabled)} role(s) assigned", Severity.Success);
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

    private async Task ConfirmEmailAsync()
    {
        try
        {
            await UserService.ConfirmEmailAsync(Id);
            _user = _user! with { EmailConfirmed = true };
            Snackbar.Add("Email confirmed", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Confirm email failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task ResendConfirmationAsync()
    {
        try
        {
            await UserService.ResendConfirmationEmailAsync(Id);
            Snackbar.Add("Confirmation email sent", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Couldn't send confirmation email: {ex.Message}", Severity.Error);
        }
    }

    private async Task RevokeOneAsync(UserSessionDto session)
    {
        _isRevokingSessions.Add(session.Id);
        try
        {
            await UserService.RevokeSessionAsync(Id, session.Id);
            _sessions!.RemoveAll(s => s.Id == session.Id);
            Snackbar.Add("Session revoked", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Revoke failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isRevokingSessions.Remove(session.Id);
        }
    }

    private async Task RevokeAllAsync()
    {
        _isRevokingAll = true;
        try
        {
            var count = await UserService.RevokeAllSessionsAsync(Id);
            _sessions = [];
            Snackbar.Add($"{count} session(s) revoked", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Revoke all failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isRevokingAll = false;
        }
    }

    private async Task ConfirmDeleteAsync()
    {
        var parameters = new DialogParameters<FshConfirmDialogContent>
        {
            { x => x.Message, $"This permanently removes {DisplayName} from the tenant. Their data is not recoverable." },
            { x => x.ConfirmText, "Delete user" },
        };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Delete user", parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        try
        {
            await UserService.DeleteAsync(Id);
            Snackbar.Add("User deleted", Severity.Success);
            Nav.NavigateTo("/identity/users");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task ConfirmImpersonateAsync()
    {
        var parameters = new DialogParameters<FshConfirmDialogContent>
        {
            { x => x.Message, "You'll act as this user for the duration of the impersonation session. All actions are audited with your operator attribution." },
            { x => x.ConfirmText, "Start impersonation" },
        };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Impersonate", parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        try
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var tenant = authState.User.FindFirst("tenant")?.Value;
            if (string.IsNullOrEmpty(tenant))
            {
                throw new InvalidOperationException("No tenant on current session");
            }

            var response = await ImpersonationService.StartImpersonationAsync(new StartImpersonationRequest(
                TargetUserId: Id,
                TargetTenantId: tenant,
                Reason: "Started from user detail"));
            await AuthState.NotifyLoginAsync(response.AccessToken, null, response.ImpersonatedTenantId);
            Snackbar.Add("Impersonation started — use the banner to end it.", Severity.Success);
            Nav.NavigateTo("/");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Impersonation failed: {ex.Message}", Severity.Error);
        }
    }

    private void GoBack() => Nav.NavigateTo("/identity/users");

    private static string SessionLabel(UserSessionDto session)
    {
        var device = string.Join(" · ", new[] { session.DeviceType, session.Browser }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        return string.IsNullOrEmpty(device) ? "Web session" : device;
    }

    private static string DeviceLabel(UserSessionDto session)
    {
        var parts = new[] { session.OperatingSystem, session.OsVersion }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        return parts.Length > 0 ? string.Join(" ", parts) : "Unknown device";
    }
}
