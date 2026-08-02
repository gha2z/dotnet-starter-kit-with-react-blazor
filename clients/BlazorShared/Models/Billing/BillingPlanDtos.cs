namespace FSH.BlazorShared.Models.Billing;

/// <summary>
/// Projection of the server's BillingPlanDto — key/currency are immutable, overage
/// rates are per-quota-resource unit prices. Interval is "Monthly" or "Yearly".
/// </summary>
public sealed record BillingPlanDto(
    Guid Id,
    string Key,
    string Name,
    string Currency,
    decimal MonthlyBasePrice,
    IReadOnlyDictionary<string, decimal> OverageRates,
    bool IsActive,
    string Interval,
    decimal? AnnualPrice);

public sealed record CreatePlanRequest(
    string Key,
    string Name,
    string Currency,
    decimal MonthlyBasePrice,
    Dictionary<string, decimal>? OverageRates = null,
    string Interval = "Monthly",
    decimal? AnnualPrice = null);

public sealed record UpdatePlanRequest(
    string Name,
    decimal MonthlyBasePrice,
    Dictionary<string, decimal>? OverageRates = null,
    string Interval = "Monthly",
    decimal? AnnualPrice = null);
