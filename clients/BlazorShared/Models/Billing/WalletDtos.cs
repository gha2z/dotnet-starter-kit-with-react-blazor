namespace FSH.BlazorShared.Models.Billing;

/// <summary>
/// Projection of the server's WalletDto. Status is "Active" | "Suspended" | "Closed".
/// </summary>
public sealed record WalletDto(
    Guid Id,
    string TenantId,
    string Currency,
    decimal Balance,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<WalletTransactionDto> RecentTransactions);

public sealed record WalletTransactionDto(
    Guid Id,
    decimal Amount,
    string Kind,
    string Description,
    string? ReferenceId,
    DateTime CreatedAtUtc);

public sealed record CreateTopupRequestRequest(
    decimal Amount,
    string? Note);
