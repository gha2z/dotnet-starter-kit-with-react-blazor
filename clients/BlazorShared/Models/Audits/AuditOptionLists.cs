namespace FSH.BlazorShared.Models.Audits;

/// <summary>
/// Filter option lists (React parity: AUDIT_EVENT_TYPES / AUDIT_SEVERITIES in api/audits.ts).
/// </summary>
public static class AuditOptionLists
{
    public static readonly string[] EventTypes =
    [
        nameof(AuditEventType.EntityChange),
        nameof(AuditEventType.Security),
        nameof(AuditEventType.Activity),
        nameof(AuditEventType.Exception),
    ];

    public static readonly string[] Severities =
    [
        nameof(AuditSeverity.Trace),
        nameof(AuditSeverity.Debug),
        nameof(AuditSeverity.Information),
        nameof(AuditSeverity.Warning),
        nameof(AuditSeverity.Error),
        nameof(AuditSeverity.Critical),
    ];
}
