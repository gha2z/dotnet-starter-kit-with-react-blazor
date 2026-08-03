using System.Net.Http.Json;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public interface ITwoFactorService
{
    Task<TwoFactorEnrollmentResponse> EnrollAsync(CancellationToken ct = default);
    Task<bool> VerifyEnrollAsync(string code, CancellationToken ct = default);
    Task<bool> DisableAsync(string currentPassword, CancellationToken ct = default);
}

public sealed class TwoFactorService(HttpClient http) : ITwoFactorService
{
    private const string IdentityBase = "/api/v1/identity";

    public async Task<TwoFactorEnrollmentResponse> EnrollAsync(CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{IdentityBase}/2fa/enroll", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TwoFactorEnrollmentResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Null 2FA enroll response");
    }

    public async Task<bool> VerifyEnrollAsync(string code, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{IdentityBase}/2fa/verify", new { code }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BoolResponse>(cancellationToken: ct);
        return result?.Success ?? false;
    }

    public async Task<bool> DisableAsync(string currentPassword, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{IdentityBase}/2fa/disable", new { currentPassword }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BoolResponse>(cancellationToken: ct);
        return result?.Success ?? false;
    }

    private sealed record BoolResponse(bool Success);
}
