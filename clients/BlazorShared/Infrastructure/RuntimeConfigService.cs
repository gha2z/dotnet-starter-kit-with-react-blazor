using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FSH.BlazorShared.Infrastructure;

public sealed class RuntimeConfigServiceStub : IRuntimeConfigService
{
    public string ApiBaseUrl => "/";
    public string DefaultTenant => "root";
    public string DashboardUrl => "http://localhost:5174";
    public int InactivityTimeoutMinutes => 10;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class RuntimeConfigService(HttpClient http, ILogger<RuntimeConfigService> logger) : IRuntimeConfigService
{
    public string ApiBaseUrl { get; private set; } = "/";
    public string DefaultTenant { get; private set; } = "root";
    public string DashboardUrl { get; private set; } = "http://localhost:5174";
    public int InactivityTimeoutMinutes { get; private set; } = 10;

    public static Uri ResolveApiBase(string appBaseAddress, string? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || apiBaseUrl == "/")
        {
            return new Uri(appBaseAddress);
        }

        return new Uri(new Uri(appBaseAddress), apiBaseUrl);
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var config = await http.GetFromJsonAsync<ConfigDto>("/config.json", ct);
            if (config is not null)
            {
                ApiBaseUrl = config.ApiBaseUrl ?? "/";
                DefaultTenant = config.DefaultTenant ?? "root";
                DashboardUrl = config.DashboardUrl ?? "http://localhost:5174";
                InactivityTimeoutMinutes = config.InactivityTimeoutMinutes ?? 10;
                logger.LogInformation("Config loaded: ApiBaseUrl={ApiBaseUrl}, DefaultTenant={DefaultTenant}", ApiBaseUrl, DefaultTenant);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load config.json, using defaults");
        }
    }

    private sealed record ConfigDto
    {
        [JsonPropertyName("apiBaseUrl")] public string? ApiBaseUrl { get; init; }
        [JsonPropertyName("defaultTenant")] public string? DefaultTenant { get; init; }
        [JsonPropertyName("dashboardUrl")] public string? DashboardUrl { get; init; }
        [JsonPropertyName("inactivityTimeoutMinutes")] public int? InactivityTimeoutMinutes { get; init; }
    }
}
