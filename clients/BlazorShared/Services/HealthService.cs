using System.Net.Http.Json;
using FSH.BlazorShared.Models.Health;

namespace FSH.BlazorShared.Services;

/// <summary>
/// Health probe service. Probes are anonymous — they bypass the auth handler and
/// tenant header so load balancers / uptime monitors can scrape them (React parity:
/// fetchHealth in api/health.ts). The readiness probe can return 503 WITH a body, so
/// the raw response is inspected before deserializing.
/// </summary>
public interface IHealthService
{
    Task<HealthResult> GetLivenessAsync(CancellationToken ct = default);
    Task<HealthResult> GetReadinessAsync(CancellationToken ct = default);
}

public sealed class HealthService(HttpClient http) : IHealthService
{
    public async Task<HealthResult> GetLivenessAsync(CancellationToken ct = default) =>
        await ProbeAsync("/health/live", ct);

    public async Task<HealthResult> GetReadinessAsync(CancellationToken ct = default) =>
        await ProbeAsync("/health/ready", ct);

    private async Task<HealthResult> ProbeAsync(string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);

        // /health/live returns 200; /health/ready returns 200 OR 503 with body.
        if (!response.IsSuccessStatusCode && (int)response.StatusCode != 503)
        {
            return new HealthResult
            {
                Status = "Unhealthy",
                Results =
                [
                    new HealthEntry
                    {
                        Name = "probe",
                        Status = "Unhealthy",
                        Description = $"Probe failed: {(int)response.StatusCode} {response.ReasonPhrase}",
                    },
                ],
            };
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return new HealthResult
            {
                Status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
            };
        }

        return await response.Content.ReadFromJsonAsync<HealthResult>(ct) ?? new HealthResult();
    }
}
