using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Tenants;

namespace FSH.BlazorShared.Services;

public interface ITenantService
{
    Task<PagedResult<TenantDto>> SearchAsync(SearchRequest request, CancellationToken ct = default);
    Task<TenantStatusDto> GetStatusAsync(string tenantId, CancellationToken ct = default);

    /// <summary>Returns null when the tenant was never run through the provisioning pipeline (404).</summary>
    Task<TenantProvisioningStatusDto?> GetProvisioningAsync(string tenantId, CancellationToken ct = default);
    Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken ct = default);
    Task<RenewTenantResponse> RenewAsync(RenewTenantRequest request, CancellationToken ct = default);
    Task<AdjustTenantValidityResponse> AdjustValidityAsync(AdjustTenantValidityRequest request, CancellationToken ct = default);
    Task<TenantLifecycleResultDto> ChangeActivationAsync(ChangeTenantActivationRequest request, CancellationToken ct = default);
    Task<TenantProvisioningStatusDto> RetryProvisioningAsync(string tenantId, CancellationToken ct = default);
}
