namespace FSH.BlazorShared.Models.Billing;

/// <summary>
/// Minimal projection of the server's InvoiceDto — enough to power the admin
/// dashboard stats. Extended in the Billing phase (2.4).
/// </summary>
public sealed record InvoiceDto(
    Guid Id,
    string TenantId,
    string InvoiceNumber,
    int PeriodYear,
    int PeriodMonth,
    string Currency,
    decimal SubtotalAmount,
    string Status,
    DateTime CreatedAtUtc,
    string Purpose);
