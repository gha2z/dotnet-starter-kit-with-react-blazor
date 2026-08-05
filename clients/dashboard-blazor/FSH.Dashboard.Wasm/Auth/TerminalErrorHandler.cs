using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using Microsoft.AspNetCore.Components;

namespace FSH.Dashboard.Wasm.Auth;

/// <summary>
/// Routes global terminal states before pages get a chance to render dead error
/// bands (React parity: the global query/mutation error hook in query-client.ts):
/// - 403 whose body carries the tenant-deactivation reason → /tenant-deactivated
/// - 401 while impersonating (grant revoked / impersonation token expired) → /impersonation-ended
///
/// Registered OUTERMOST (first AddHttpMessageHandler) so it sees the FINAL response,
/// including refresh outcomes from AuthDelegatingHandler, and the ApiRequestException
/// that handler throws when refresh fails.
/// </summary>
public sealed class TerminalErrorHandler(ITokenStore tokenStore, NavigationManager nav) : DelegatingHandler
{
    private static readonly string[] TerminalPaths = ["/tenant-deactivated", "/impersonation-ended"];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Capture before the call: AuthDelegatingHandler clears the store on refresh failure.
        var wasImpersonating = await WasImpersonatingAsync();

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (ApiRequestException ex) when (ex.StatusCode == 401 && wasImpersonating)
        {
            nav.NavigateTo("/impersonation-ended");
            throw;
        }

        if (response.IsSuccessStatusCode || IsOnTerminalPath())
        {
            return response;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            // Read once and re-materialize so downstream deserializers still see the body.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Content = new StringContent(body, Encoding.UTF8, response.Content.Headers.ContentType?.MediaType ?? "application/json");
            if (body.Contains("tenant has been deactivated", StringComparison.OrdinalIgnoreCase))
            {
                nav.NavigateTo("/tenant-deactivated");
            }
        }
        else if (wasImpersonating && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            nav.NavigateTo("/impersonation-ended");
        }

        return response;
    }

    private async Task<bool> WasImpersonatingAsync()
    {
        try
        {
            var token = await tokenStore.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.Claims.Any(c => c.Type == "act_sub");
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool IsOnTerminalPath()
        => TerminalPaths.Any(p => nav.Uri.Contains(p, StringComparison.OrdinalIgnoreCase));
}
