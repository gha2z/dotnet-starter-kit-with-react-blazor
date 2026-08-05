using System.Net.Http.Json;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public interface IImpersonationService
{
    Task<List<ImpersonationGrantDto>> ListGrantsAsync(
        ImpersonationGrantStatus? status = null,
        string? impersonatedTenantId = null,
        string? actorUserId = null,
        int take = 200,
        CancellationToken ct = default);
    Task<ImpersonationResponse> StartImpersonationAsync(StartImpersonationRequest request, CancellationToken ct = default);
    Task<ImpersonationGrantDto> RevokeGrantAsync(Guid grantId, string? reason, CancellationToken ct = default);
    Task<TokenResponse> EndImpersonationAsync(CancellationToken ct = default);
}

public sealed class ImpersonationService(HttpClient http) : IImpersonationService
{
    private const string Base = "/api/v1/identity/impersonation";

    public async Task<List<ImpersonationGrantDto>> ListGrantsAsync(
        ImpersonationGrantStatus? status = null,
        string? impersonatedTenantId = null,
        string? actorUserId = null,
        int take = 200,
        CancellationToken ct = default)
    {
        var query = $"Take={take}";
        if (status.HasValue)
            query += $"&Status={status.Value}";
        if (!string.IsNullOrWhiteSpace(impersonatedTenantId))
            query += $"&ImpersonatedTenantId={Uri.EscapeDataString(impersonatedTenantId)}";
        if (!string.IsNullOrWhiteSpace(actorUserId))
            query += $"&ActorUserId={Uri.EscapeDataString(actorUserId)}";

        return await http.GetFromJsonAsync<List<ImpersonationGrantDto>>($"{Base}/grants?{query}", ct) ?? [];
    }

    public async Task<ImpersonationResponse> StartImpersonationAsync(StartImpersonationRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/start", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ImpersonationResponse>(ct)
            ?? throw new InvalidOperationException("Null impersonation start response");
    }

    public async Task<ImpersonationGrantDto> RevokeGrantAsync(Guid grantId, string? reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/grants/{grantId}/revoke", new { reason }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ImpersonationGrantDto>(ct)
            ?? throw new InvalidOperationException("Null revoke response");
    }

    /// <summary>
    /// Ends the active impersonation grant. Returns a fresh operator token pair when
    /// the operator had a stashed dashboard session (React parity: stopImpersonation).
    /// </summary>
    public async Task<TokenResponse> EndImpersonationAsync(CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{Base}/end", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
            ?? throw new InvalidOperationException("Null end-impersonation response");
    }
}