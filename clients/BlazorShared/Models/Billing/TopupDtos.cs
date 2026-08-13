namespace FSH.BlazorShared.Models.Billing;

/// <summary>
/// Projection of the server's TopupRequestDto. Status is one of
/// "Pending" | "Invoiced" | "Completed" | "Rejected" | "Cancelled".
/// </summary>
public sealed record TopupRequestDto(
    Guid Id,
    string TenantId,
    decimal Amount,
    string Currency,
    string? Note,
    string Status,
    Guid? InvoiceId,
    string? RequestedBy,
    string? DecisionNote,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc,
    DateTime? CompletedAtUtc);
