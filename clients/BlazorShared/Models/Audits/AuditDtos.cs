using System.Text.Json;
using System.Text.Json.Serialization;
using FSH.BlazorShared.Models.Json;

namespace FSH.BlazorShared.Models.Audits;

/// <summary>Serialized as its string name by the API's global JsonStringEnumConverter
/// (e.g. "EntityChange") — integer values are also accepted defensively.</summary>
[JsonConverter(typeof(FlexibleEnumJsonConverter<AuditEventType>))]
public enum AuditEventType
{
    None = 0,
    EntityChange = 1,
    Security = 2,
    Activity = 3,
    Exception = 4
}

/// <summary>Serialized as its string name (e.g. "Information") — integers accepted too.</summary>
[JsonConverter(typeof(FlexibleEnumJsonConverter<AuditSeverity>))]
public enum AuditSeverity
{
    None = 0,
    Trace = 1,
    Debug = 2,
    Information = 3,
    Warning = 4,
    Error = 5,
    Critical = 6
}

/// <summary>Serialized as its string name or comma-separated names ("PiiMasked",
/// "PiiMasked, Sampled") by the API's global JsonStringEnumConverter — integers
/// accepted too.</summary>
[JsonConverter(typeof(FlexibleEnumJsonConverter<AuditTag>))]
[Flags]
public enum AuditTag
{
    None = 0,
    PiiMasked = 1 << 0,
    OutOfQuota = 1 << 1,
    Sampled = 1 << 2,
    RetainedLong = 1 << 3,
    HealthCheck = 1 << 4,
    Authentication = 1 << 5,
    Authorization = 1 << 6
}

/// <summary>
/// Row projection of an audit event (React parity: AuditSummaryDto).
/// </summary>
public class AuditSummaryDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public AuditEventType EventType { get; set; }
    public AuditSeverity Severity { get; set; }
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? Source { get; set; }
    public AuditTag Tags { get; set; }
}

/// <summary>
/// Full audit record including the free-form payload (React parity: AuditDetailDto).
/// </summary>
public sealed class AuditDetailDto : AuditSummaryDto
{
    public DateTime ReceivedAtUtc { get; set; }
    public string? SpanId { get; set; }
    public JsonElement Payload { get; set; }
}

public sealed class AuditSummaryAggregateDto
{
    public Dictionary<string, long> EventsByType { get; set; } = [];
    public Dictionary<string, long> EventsBySeverity { get; set; } = [];
    public Dictionary<string, long> EventsBySource { get; set; } = [];
    public Dictionary<string, long> EventsByTenant { get; set; } = [];
}

public sealed class ListAuditsRequest
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public AuditEventType? EventType { get; set; }

    /// <summary>Hide a single event type (e.g. Activity to drop system-level HTTP noise).</summary>
    public AuditEventType? ExcludeEventType { get; set; }

    public AuditSeverity? Severity { get; set; }

    /// <summary>Bitmask of <see cref="AuditTag"/> values.</summary>
    public AuditTag? Tags { get; set; }

    public string? Source { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? Search { get; set; }
}
