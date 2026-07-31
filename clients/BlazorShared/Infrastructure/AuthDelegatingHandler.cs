using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FSH.BlazorShared.Auth;

namespace FSH.BlazorShared.Infrastructure;

public sealed class AuthDelegatingHandler(ITokenStore tokenStore, IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var tenant = await tokenStore.GetTenantAsync();
        if (tenant is not null)
        {
            request.Headers.TryAddWithoutValidation("tenant", tenant);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refresh = await tokenStore.GetRefreshTokenAsync();
            if (refresh is not null)
            {
                await RefreshLock.WaitAsync(cancellationToken);
                try
                {
                    var newToken = await RefreshAccessTokenAsync(token!, refresh, cancellationToken);
                    await tokenStore.SetTokensAsync(newToken.Token, newToken.RefreshToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken.Token);

                    // Dispose old response before retry
                    response.Dispose();
                    response = await base.SendAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    await tokenStore.ClearAsync();
                    throw new ApiRequestException(401, $"Session expired: {ex.Message}");
                }
                finally
                {
                    RefreshLock.Release();
                }
            }
        }

        return response;
    }

    private async Task<RefreshResponse> RefreshAccessTokenAsync(string token, string refreshToken, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient("FSH.Auth");
        var tenant = await tokenStore.GetTenantAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/token/refresh")
        {
            Content = JsonContent.Create(new { token, refreshToken }),
        };
        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RefreshResponse>(ct)
               ?? throw new InvalidOperationException("Null refresh response");
    }

    private sealed record RefreshResponse(string Token, string? RefreshToken);
}

public sealed class ApiRequestException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
