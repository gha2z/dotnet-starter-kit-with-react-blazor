namespace FSH.BlazorShared.Models.Billing;

/// <summary>
/// Minimal projection of the server's BillingPlanDto — enough to power the tenant
/// create/renew plan selectors. Extended in the Billing phase (2.4).
/// </summary>
public sealed record BillingPlanDto(
    Guid Id,
    string Key,
    string Name,
    string Currency,
    decimal MonthlyBasePrice,
    bool IsActive,
    string Interval,
    decimal? AnnualPrice);
