using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FSH.BlazorShared.Models.Tenants;

namespace FSH.BlazorShared.Services;

public sealed class TenantThemeService(HttpClient http) : ITenantThemeService
{
    private const string ThemeBase = "/api/v1/tenants/theme";

    public async Task<TenantThemeDto> GetThemeAsync(string tenantId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ThemeBase);
        request.Headers.TryAddWithoutValidation("tenant", tenantId);
        var response = await http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantThemeDto>(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Null tenant theme response");
    }

    public async Task UpdateThemeAsync(string tenantId, TenantThemeDto theme, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, ThemeBase)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(theme),
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("tenant", tenantId);
        var response = await http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetThemeAsync(string tenantId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ThemeBase}/reset");
        request.Headers.TryAddWithoutValidation("tenant", tenantId);
        var response = await http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
