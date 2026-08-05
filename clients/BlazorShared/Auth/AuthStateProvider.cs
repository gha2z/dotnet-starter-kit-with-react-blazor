using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FSH.BlazorShared.Models.Identity;
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

    /// <summary>
    /// Detects an active impersonation session from the JWT's actor claims
    /// (act_sub / act_tenant / act_name) and exposes the impersonation context.
    /// Null when the current identity is a regular sign-in. React parity:
    /// the same claims feed ImpersonationInfo in auth-context.ts.
    /// </summary>
    public async Task<ImpersonationInfo?> GetImpersonationAsync()
        => GetImpersonation((await GetAuthenticationStateAsync()).User);

    public static ImpersonationInfo? GetImpersonation(ClaimsPrincipal user)
    {
        var actor = user.FindFirst("act_sub")?.Value;
        if (actor is null)
        {
            return null;
        }

        return new ImpersonationInfo(
            ActorUserId: actor,
            ActorTenantId: user.FindFirst("act_tenant")?.Value ?? string.Empty,
            ActorName: user.FindFirst("act_name")?.Value ?? "Operator",
            SubjectUserId: user.FindFirst("sub")?.Value ?? string.Empty,
            SubjectTenantId: user.FindFirst("tenant")?.Value ?? string.Empty,
            SubjectName: user.FindFirst("name")?.Value ?? user.FindFirst("email")?.Value ?? string.Empty);
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
