using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FSH.BlazorShared.Auth;

public sealed class AuthStateProvider(ITokenStore tokenStore, IPermissionsProvider permissionsProvider)
    : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> AnonymousState =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
        {
            return await AnonymousState;
        }

        var principal = await CreatePrincipalAsync(token);
        return new AuthenticationState(principal);
    }

    public async Task NotifyLoginAsync(string accessToken, string? refreshToken, string tenant)
    {
        await tokenStore.SetTokensAsync(accessToken, refreshToken);
        await tokenStore.SetTenantAsync(tenant);
        await permissionsProvider.ResetAsync();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLogoutAsync()
    {
        await tokenStore.ClearAsync();
        await permissionsProvider.ResetAsync();
        NotifyAuthenticationStateChanged(AnonymousState);
    }

    /// <summary>
    /// Re-raises the state-changed notification after clearing the permission cache, so
    /// gates and navigation re-evaluate against freshly hydrated claims. React parity:
    /// the JWT only carries roles - permissions are resolved server-side per role.
    /// </summary>
    public async Task RefreshAsync()
    {
        await permissionsProvider.InvalidateCache();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");

        var roleClaims = identity.FindAll(c => c.Type is "role" or "roles" or ClaimTypes.Role).ToList();
        foreach (var roleClaim in roleClaims)
        {
            if (roleClaim.Type != ClaimTypes.Role)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
            }
        }

        foreach (var permission in await permissionsProvider.GetPermissionsAsync())
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        return new ClaimsPrincipal(identity);
    }

    private static bool IsTokenExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.ValidTo <= DateTime.UtcNow.AddSeconds(10);
    }
}
