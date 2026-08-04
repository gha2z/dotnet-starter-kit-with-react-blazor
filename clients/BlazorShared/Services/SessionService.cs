using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public interface ISessionService
{
    // Self-service
    Task<List<UserSessionDto>> GetMySessionsAsync(CancellationToken ct = default);
    Task RevokeMySessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<int> RevokeAllMySessionsAsync(CancellationToken ct = default);

    // Admin / tenant-wide
    Task<PagedResult<UserSessionDto>> GetTenantSessionsAsync(
        string? search = null,
        bool? includeInactive = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default);
    Task AdminRevokeUserSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<int> AdminRevokeAllUserSessionsAsync(Guid userId, CancellationToken ct = default);
}

public sealed class SessionService(HttpClient http) : ISessionService
{
    private const string IdentityBase = "/api/v1/identity";

    public async Task<List<UserSessionDto>> GetMySessionsAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<UserSessionDto>>($"{IdentityBase}/sessions/me", ct) ?? [];
    }

    public async Task RevokeMySessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{IdentityBase}/sessions/{sessionId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> RevokeAllMySessionsAsync(CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{IdentityBase}/sessions/revoke-all", new { }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RevokeAllSessionsResponse>(cancellationToken: ct);
        return result?.RevokedCount ?? 0;
    }

    public async Task<PagedResult<UserSessionDto>> GetTenantSessionsAsync(
        string? search = null,
        bool? includeInactive = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var parts = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}",
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (includeInactive.HasValue)
        {
            parts.Add($"includeInactive={includeInactive.Value.ToString().ToLowerInvariant()}");
        }

        var query = string.Join("&", parts);
        return await http.GetFromJsonAsync<PagedResult<UserSessionDto>>($"{IdentityBase}/sessions?{query}", ct)
            ?? new PagedResult<UserSessionDto>([], pageNumber, pageSize, 0, 0, false, false);
    }

    public async Task AdminRevokeUserSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{IdentityBase}/users/{userId}/sessions/{sessionId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> AdminRevokeAllUserSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{IdentityBase}/users/{userId}/sessions/revoke-all", new { }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RevokeAllSessionsResponse>(cancellationToken: ct);
        return result?.RevokedCount ?? 0;
    }

    private sealed record RevokeAllSessionsResponse(int RevokedCount);
}
