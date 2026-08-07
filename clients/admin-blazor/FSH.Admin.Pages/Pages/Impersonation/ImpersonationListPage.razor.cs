using System.Security.Claims;
using FSH.BlazorShared.Infrastructure;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Impersonation;

public sealed partial class ImpersonationListPage
{
    [Inject] private IImpersonationService ImpersonationService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private IRuntimeConfigService Config { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private readonly List<ImpersonationGrantDto> _grants = [];
    private readonly Dictionary<ImpersonationGrantStatus, int> _byStatus = new()
    {
        [ImpersonationGrantStatus.Active] = 0,
        [ImpersonationGrantStatus.Ended] = 0,
        [ImpersonationGrantStatus.Revoked] = 0,
        [ImpersonationGrantStatus.Expired] = 0,
    };

    private string _filter = ((int)ImpersonationGrantStatus.Active).ToString();
    private string? _error;
    private bool _loading = true;
    private bool _isFetching;
    private string? _currentUserId;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? state.User.FindFirst("sub")?.Value;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isFetching)
        {
            return;
        }

        _isFetching = true;
        _loading = _grants.Count == 0;
        _error = null;
        try
        {
            ImpersonationGrantStatus? status = string.IsNullOrEmpty(_filter) || _filter == "all" ? null : (ImpersonationGrantStatus?)int.Parse(_filter);
            var items = await ImpersonationService.ListGrantsAsync(status: status, take: 200);

            foreach (var key in _byStatus.Keys.ToList())
            {
                _byStatus[key] = 0;
            }

            foreach (var g in items)
            {
                _byStatus[g.Status] = _byStatus[g.Status] + 1;
            }

            _grants.Clear();
            _grants.AddRange(items);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isFetching = false;
            _loading = false;
        }
    }

    private bool IsCurrentRow(ImpersonationGrantDto grant) =>
        _currentUserId is not null && grant.ActorUserId == _currentUserId;

    private async Task OnFilterChangedAsync(string? value)
    {
        _filter = value ?? string.Empty;
        await LoadAsync();
    }

    private async Task ReopenAsync(ImpersonationGrantDto grant)
    {
        if (!IsCurrentRow(grant))
        {
            return;
        }

        var parameters = new DialogParameters
        {
            { "TargetTenantId", grant.ImpersonatedTenantId },
            { "PrefillUser", SynthesizeUser(grant) },
        };
        await ShowImpersonateDialogAsync("Re-open impersonation", parameters);
    }

    private async Task ShowImpersonateDialogAsync(string title, DialogParameters parameters)
    {
        var dialog = await DialogService.ShowAsync<ImpersonateDialog>(title, parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true });
        var result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not ImpersonationResponse response)
        {
            return;
        }

        var tenantId = parameters["TargetTenantId"] as string ?? string.Empty;
        var url = BuildHandoffUrl(response, tenantId);
        await Js.InvokeVoidAsync("openUrl", url);
        Snackbar.Add("Opened the dashboard as the impersonated user. End impersonation from inside the dashboard tab.", Severity.Info);
        await LoadAsync();
    }

    private async Task RevokeAsync(ImpersonationGrantDto grant)
    {
        var parameters = new DialogParameters { { "Grant", grant } };
        var dialog = await DialogService.ShowAsync<RevokeGrantDialog>("Revoke impersonation grant", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await LoadAsync();
    }

    private string BuildHandoffUrl(ImpersonationResponse response, string tenantId)
    {
        var query = $"token={Uri.EscapeDataString(response.AccessToken)}" +
                    $"&tenant={Uri.EscapeDataString(tenantId)}" +
                    $"&expiresAt={Uri.EscapeDataString(response.AccessTokenExpiresAt.ToString("o"))}";
        return $"{Config.DashboardUrl.TrimEnd('/')}/#impersonate?{query}";
    }

    private static UserDto SynthesizeUser(ImpersonationGrantDto grant) =>
        new(
            grant.ImpersonatedUserId,
            grant.ImpersonatedUserName,
            null,
            null,
            null,
            true,
            false,
            null,
            null,
            false);

    private static string StatusDot(ImpersonationGrantStatus status) => status switch
    {
        ImpersonationGrantStatus.Active => "fsh-dot-active",
        ImpersonationGrantStatus.Ended => "fsh-dot-muted",
        ImpersonationGrantStatus.Revoked => "fsh-dot-error",
        ImpersonationGrantStatus.Expired => "fsh-dot-muted",
        _ => "fsh-dot-muted",
    };

    private static Color StatusTone(ImpersonationGrantStatus status) => status switch
    {
        ImpersonationGrantStatus.Active => Color.Success,
        ImpersonationGrantStatus.Ended => Color.Info,
        ImpersonationGrantStatus.Revoked => Color.Error,
        ImpersonationGrantStatus.Expired => Color.Default,
        _ => Color.Default,
    };
}