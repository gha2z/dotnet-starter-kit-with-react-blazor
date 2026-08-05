using FSH.BlazorShared.Infrastructure;

namespace FSH.Hybrid.Services;

public sealed class HybridRuntimeConfigService : IRuntimeConfigService
{
    private const string ApiBaseKey = "fsh.hybrid.apiBase";
    private const string DefaultApiBase = "https://localhost:7030";

    public string ApiBaseUrl { get; set; }
    public string DefaultTenant => "root";
    public string DashboardUrl => "https://localhost:5174";
    public int InactivityTimeoutMinutes => 10;

    public HybridRuntimeConfigService()
    {
        ApiBaseUrl = Preferences.Default.Get(ApiBaseKey, DefaultApiBase);
    }

    public Task LoadAsync(CancellationToken ct = default)
    {
        // The hybrid shell has no /config.json host page; the API base comes from
        // Preferences (settable on the native Settings page). Android emulators
        // should override it to http://10.0.2.2:5030.
        return Task.CompletedTask;
    }
}
