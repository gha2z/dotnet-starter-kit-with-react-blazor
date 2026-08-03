using System.Text.Json.Serialization;

namespace FSH.BlazorShared.Models.Dashboard;

public sealed record TenantStatusDto(
    string Id,
    string Name,
    bool IsActive,
    string ValidUpto,
    string ExpiryState,
    string GraceEndsUtc,
    bool HasConnectionString,
    string AdminEmail,
    string? Issuer,
    string? Plan);

public sealed record SubscriptionDto(
    Guid Id,
    Guid PlanId,
    string PlanKey,
    DateTime StartUtc,
    DateTime? EndUtc,
    string Status);

public sealed record UsageSnapshotDto(
    Guid Id,
    string TenantId,
    int PeriodYear,
    int PeriodMonth,
    string Resource,
    long UsedUnits,
    long LimitUnits,
    long Overage,
    DateTime CapturedAtUtc);