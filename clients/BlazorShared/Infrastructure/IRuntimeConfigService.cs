namespace FSH.BlazorShared.Infrastructure;

public interface IRuntimeConfigService
{
    string ApiBaseUrl { get; }
    string DefaultTenant { get; }
    int InactivityTimeoutMinutes { get; }
    Task LoadAsync(CancellationToken ct = default);
}
