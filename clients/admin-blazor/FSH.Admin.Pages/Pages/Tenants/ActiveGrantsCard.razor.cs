using System.Security.Claims;
using FSH.Admin.Wasm.Pages.Impersonation;
using FSH.BlazorShared.Infrastructure;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Tenants;

public sealed partial class ActiveGrantsCard : IAsyncDisposable
{
    [Parameter] public string TenantId { get; set; } = string.Empty;

    [Inject] private IImpersonationService ImpersonationService { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IRuntimeConfigService Config { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private readonly List<ImpersonationGrantDto> _grants = [];
    private string? _currentUserId;
    private bool _canView;
    private bool _canRevoke;
    private bool _canReopen;
    private string? _error;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

        _canView = await AuthorizeAsync(state, IdentityPermissions.Impersonation.View);
        _canRevoke = await AuthorizeAsync(state, IdentityPermissions.Impersonation.Revoke);
        _canReopen = await AuthorizeAsync(state, IdentityPermissions.Users.Impersonate);

        if (!_canView)
        {
            return;
        }

        await LoadAsync();
        StartPolling();
    }

    private async Task<bool> AuthorizeAsync(AuthenticationState state, string permission) =>
        (await AuthorizationService.AuthorizeAsync(state.User, permission)).Succeeded;

    private void StartPolling()
    {
        _pollCts = new CancellationTokenSource();
        _pollTask = PollLoopAsync(_pollCts.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await LoadAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed — stop polling quietly.
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var items = await ImpersonationService.ListGrantsAsync(
                status: ImpersonationGrantStatus.Active,
                impersonatedTenantId: TenantId,
                take: 50);

            _grants.Clear();
            _grants.AddRange(items);
            _error = null;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load active impersonations: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task ReopenAsync(ImpersonationGrantDto grant)
    {
        var parameters = new DialogParameters
        {
            { "TargetTenantId", grant.ImpersonatedTenantId },
            { "PrefillUser", SynthesizeUser(grant) },
        };
        var dialog = await DialogService.ShowAsync<ImpersonateDialog>("Re-open impersonation", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true });
        var result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not ImpersonationResponse response)
        {
            return;
        }

        await OpenHandoffAsync(response, grant.ImpersonatedTenantId);
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

    private async Task OpenHandoffAsync(ImpersonationResponse response, string tenantId)
    {
        var query = $"token={Uri.EscapeDataString(response.AccessToken)}" +
                    $"&tenant={Uri.EscapeDataString(tenantId)}" +
                    $"&expiresAt={Uri.EscapeDataString(response.AccessTokenExpiresAt.ToString("o"))}";
        var url = $"{Config.DashboardUrl.TrimEnd('/')}/#impersonate?{query}";
        await Js.InvokeVoidAsync("openUrl", url);
        Snackbar.Add("Opened the dashboard as the impersonated user. End impersonation from inside the dashboard tab.", Severity.Info);
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

    private static string Truncate(string value, int max) =>
        value.Length > max ? $"{value[..(max - 1)]}…" : value;

    public async ValueTask DisposeAsync()
    {
        if (_pollCts is not null)
        {
            await _pollCts.CancelAsync();
            _pollCts.Dispose();
        }

        if (_pollTask is not null)
        {
            try
            {
                await _pollTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected — polling was cancelled.
            }
        }
    }
}
