namespace FSH.BlazorShared.Models.Health;

/// <summary>
/// Health probe result (React parity: HealthResult in api/health.ts).
/// </summary>
public sealed class HealthResult
{
    public string Status { get; set; } = "Unknown";
    public List<HealthEntry> Results { get; set; } = [];
}

public sealed class HealthEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double DurationMs { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}
