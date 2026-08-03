using System.Net.Http.Json;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public interface ISessionService
{
    Task<List<UserSessionDto>> GetMySessionsAsync(CancellationToken ct = default);
    Task RevokeMySessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<int> RevokeAllMySessionsAsync(CancellationToken ct = default);
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

    private sealed record RevokeAllSessionsResponse(int RevokedCount);
}
