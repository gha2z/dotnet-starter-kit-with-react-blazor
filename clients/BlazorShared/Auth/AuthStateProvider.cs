using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FSH.BlazorShared.Auth;

public sealed class AuthStateProvider(ITokenStore tokenStore) : AuthenticationStateProvider
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

        var principal = CreatePrincipal(token);
        return new AuthenticationState(principal);
    }

    public async Task NotifyLoginAsync(string accessToken, string? refreshToken, string tenant)
    {
        await tokenStore.SetTokensAsync(accessToken, refreshToken);
        await tokenStore.SetTenantAsync(tenant);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLogoutAsync()
    {
        await tokenStore.ClearAsync();
        NotifyAuthenticationStateChanged(AnonymousState);
    }

    private static ClaimsPrincipal CreatePrincipal(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private static bool IsTokenExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.ValidTo <= DateTime.UtcNow.AddSeconds(10);
    }
}
