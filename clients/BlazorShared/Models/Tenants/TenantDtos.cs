namespace FSH.BlazorShared.Models.Tenants;

public sealed class TenantDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? ConnectionString { get; set; }
    public string AdminEmail { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime ValidUpto { get; set; }
    public string? Issuer { get; set; }
}

public sealed class TenantStatusDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public bool IsActive { get; init; }
    public DateTime ValidUpto { get; init; }
    public bool HasConnectionString { get; init; }
    public string AdminEmail { get; init; } = default!;
    public string? Issuer { get; init; }

    /// <summary>The tenant's current billing plan key (drives quotas + subscription).</summary>
    public string? Plan { get; init; }

    /// <summary>Derived lifecycle state: "Active", "InGrace", or "Expired".</summary>
    public string ExpiryState { get; init; } = "Active";

    /// <summary>Instant after which a lapsed tenant is hard-blocked (ValidUpto + grace period).</summary>
    public DateTime GraceEndsUtc { get; init; }
}

public sealed record CreateTenantRequest(
    string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string AdminPassword,
    string? Issuer,
    string? PlanKey = null);

public sealed record CreateTenantResponse(
    string Id,
    string ProvisioningCorrelationId,
    string Status);

public sealed record RenewTenantRequest(string TenantId, string? PlanKey = null);

public sealed record RenewTenantResponse(
    string TenantId,
    DateTime ValidUpto,
    string PlanKey,
    bool PlanChanged);

public sealed record AdjustTenantValidityRequest(string TenantId, DateTime ValidUpto);

public sealed record AdjustTenantValidityResponse(string TenantId, DateTime ValidUpto);

public sealed record ChangeTenantActivationRequest(string TenantId, bool IsActive);

public sealed class TenantLifecycleResultDto
{
    public string TenantId { get; set; } = default!;

    public bool IsActive { get; set; }

    public DateTime? ValidUpto { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed record TenantProvisioningStepDto(
    string Step,
    string Status,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    string? Error);

public sealed record TenantProvisioningStatusDto(
    string TenantId,
    string Status,
    string CorrelationId,
    string? CurrentStep,
    string? Error,
    DateTime CreatedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    IReadOnlyCollection<TenantProvisioningStepDto> Steps);
