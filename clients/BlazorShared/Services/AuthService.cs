using System.Net.Http.Headers;
using System.Net.Http.Json;
using FSH.BlazorShared.Auth;

namespace FSH.BlazorShared.Services;

public sealed class AuthService(
    IHttpClientFactory httpClientFactory,
    ITokenStore tokenStore) : IAuthService
{
    private const string ClientName = "FSH.Auth";

    public async Task<TokenResponse> LoginAsync(string email, string password, string tenant, string appHeader, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient(ClientName);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/token/issue")
        {
            Content = JsonContent.Create(new { email, password }),
        };
        request.Headers.TryAddWithoutValidation("tenant", tenant);
        request.Headers.TryAddWithoutValidation("X-FSH-App", appHeader);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        return result ?? throw new InvalidOperationException("Null login response");
    }

    public async Task<RefreshResponse> RefreshAsync(string accessToken, string refreshToken, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient(ClientName);

        var tenant = await tokenStore.GetTenantAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/token/refresh")
        {
            Content = JsonContent.Create(new { token = accessToken, refreshToken }),
        };
        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RefreshResponse>(ct);
        return result ?? throw new InvalidOperationException("Null refresh response");
    }

    public async Task<string[]> GetPermissionsAsync(CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient(ClientName);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/permissions");
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var tenant = await tokenStore.GetTenantAsync();
        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<string[]>(ct) ?? [];
    }

    public async Task ForgotPasswordAsync(string email, string tenant, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient(ClientName);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/forgot-password")
        {
            Content = JsonContent.Create(new { email }),
        };
        request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetPasswordAsync(string email, string password, string token, string tenant, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient(ClientName);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/reset-password")
        {
            Content = JsonContent.Create(new { email, password, token }),
        };
        request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient(ClientName);

        var url = $"/api/v1/identity/confirm-email?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}&tenant={Uri.EscapeDataString(tenant)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }
}
