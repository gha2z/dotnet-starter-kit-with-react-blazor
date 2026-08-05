using System.IdentityModel.Tokens.Jwt;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Auth;

public sealed partial class ImpersonationBanner : IDisposable
{
    [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
    [Inject] private AuthStateProvider AuthState { get; set; } = default!;
    [Inject] private IImpersonationService ImpersonationService { get; set; } = default!;
    [Inject] private ITokenStore TokenStore { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private ImpersonationInfo? _info;
    private bool _crossTenant;
    private bool _ending;

    protected override void OnInitialized()
    {
        Auth.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    protected override async Task OnInitializedAsync()
        => await EvaluateAsync(Auth.GetAuthenticationStateAsync());

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        => await EvaluateAsync(task);

    private async Task EvaluateAsync(Task<AuthenticationState> task)
    {
        try
        {
            var state = await task;
            _info = AuthStateProvider.GetImpersonation(state.User);
            // Tone scaling (React parity): same-tenant impersonation is amber, a
            // cross-tenant session (root/SuperAdmin stepping into another tenant) is red.
            _crossTenant = _info is not null
                && !string.Equals(_info.ActorTenantId, _info.SubjectTenantId, StringComparison.OrdinalIgnoreCase);
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Auth state task faulted (e.g. malformed token) - keep the previous state.
        }
    }

    private async Task StopImpersonationAsync()
    {
        if (_ending)
        {
            return;
        }

        _ending = true;
        try
        {
            if (!await TokenStore.HasImpersonationStashAsync())
            {
                // No stashed operator session (e.g. the dashboard tab was opened from a fresh
                // browser): end the grant on the server best-effort and sign straight out.
                // React parity: fire-and-forget endImpersonation() then logout.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ImpersonationService.EndImpersonationAsync();
                    }
                    catch (Exception)
                    {
                        // Best effort - the grant may already be revoked or expired server-side.
                    }
                });
                await AuthState.NotifyLogoutAsync();
                Nav.NavigateTo("/login");
                return;
            }

            TokenResponse fresh;
            try
            {
                fresh = await ImpersonationService.EndImpersonationAsync();
            }
            catch (Exception ex)
            {
                // Server refused to end the grant: fall back to the stashed operator session.
                await TokenStore.RestoreTokensAsync();
                await AuthState.RefreshAsync();
                Snackbar.Add($"Could not end impersonation: {ex.Message}", Severity.Error);
                return;
            }

            if (IsRootTenant(fresh.AccessToken))
            {
                // Root operator (SuperAdmin) keeps one identity across tenants - ending the
                // impersonation must not overwrite their session with tenant-scoped tokens.
                await AuthState.NotifyLogoutAsync();
                Nav.NavigateTo("/login");
                return;
            }

            await TokenStore.SetFreshTokensAsync(fresh.AccessToken, fresh.RefreshToken);
            await AuthState.RefreshAsync();
            Nav.NavigateTo("/");
        }
        finally
        {
            _ending = false;
        }
    }

    private static bool IsRootTenant(string accessToken)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            return jwt.Claims.FirstOrDefault(c => c.Type == "tenant")?.Value == "root";
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose()
        => Auth.AuthenticationStateChanged -= OnAuthenticationStateChanged;
}
