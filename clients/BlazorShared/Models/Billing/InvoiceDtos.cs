namespace FSH.BlazorShared.Models.Billing;

/// <summary>
/// One line of an invoice. Kind is "BaseFee" | "Overage" | "Adjustment";
/// Resource is a QuotaResource name ("ApiCalls", "StorageBytes", "Users",
/// "ActiveFeatureFlags") or null for non-resource lines.
/// </summary>
public sealed record InvoiceLineItemDto(
    Guid Id,
    string Kind,
    string? Resource,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount);

/// <summary>
/// Projection of the server's InvoiceDto. Status is "Draft" | "Issued" | "Paid" | "Void";
/// Purpose is "Usage" | "Subscription" | "Topup".
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
    DateTime? IssuedAtUtc,
    DateTime? DueAtUtc,
    DateTime? PaidAtUtc,
    DateTime? VoidedAtUtc,
    string? Notes,
    List<InvoiceLineItemDto> LineItems,
    string Purpose,
    DateTime? PeriodStartUtc,
    DateTime? PeriodEndUtc);
