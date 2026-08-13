using System.Net;
using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Tenants;

namespace FSH.BlazorShared.Services;

public sealed class TenantService(HttpClient http) : ITenantService
{
    private const string TenantsBase = "/api/v1/tenants";

    public async Task<PagedResult<TenantDto>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var query = $"PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.SortBy))
            query += $"&Sort={Uri.EscapeDataString(request.SortBy)}";

        return await http.GetFromJsonAsync<PagedResult<TenantDto>>($"{TenantsBase}/?{query}", ct)
            ?? new PagedResult<TenantDto>([], request.PageNumber, request.PageSize, 0, 0, false, false);
    }

    public async Task<TenantStatusDto> GetStatusAsync(string tenantId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<TenantStatusDto>($"{TenantsBase}/{tenantId}/status", ct)
            ?? throw new InvalidOperationException("Null tenant status response");
    }

    public async Task<TenantProvisioningStatusDto?> GetProvisioningAsync(string tenantId, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<TenantProvisioningStatusDto>(
                $"{TenantsBase}/{tenantId}/provisioning", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{TenantsBase}/", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateTenantResponse>(ct)
            ?? throw new InvalidOperationException("Null create response");
    }

    public async Task<RenewTenantResponse> RenewAsync(RenewTenantRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{TenantsBase}/{request.TenantId}/renew", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RenewTenantResponse>(ct)
            ?? throw new InvalidOperationException("Null renew response");
    }

    public async Task<AdjustTenantValidityResponse> AdjustValidityAsync(AdjustTenantValidityRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{TenantsBase}/{request.TenantId}/adjust-validity", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdjustTenantValidityResponse>(ct)
            ?? throw new InvalidOperationException("Null adjust response");
    }

    public async Task<TenantLifecycleResultDto> ChangeActivationAsync(ChangeTenantActivationRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{TenantsBase}/{request.TenantId}/activation", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantLifecycleResultDto>(ct)
            ?? throw new InvalidOperationException("Null activation response");
    }

    public async Task<TenantProvisioningStatusDto> RetryProvisioningAsync(string tenantId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{TenantsBase}/{tenantId}/provisioning/retry", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantProvisioningStatusDto>(ct)
            ?? throw new InvalidOperationException("Null retry response");
    }
}
