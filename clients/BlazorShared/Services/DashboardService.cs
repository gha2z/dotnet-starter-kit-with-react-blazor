using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Dashboard;
using FSH.BlazorShared.Models.Audits;
namespace FSH.BlazorShared.Services;

public interface IDashboardService
{
    Task<TenantStatusDto> GetMyTenantStatusAsync(CancellationToken ct = default);
    Task<SubscriptionDto?> GetMySubscriptionAsync(CancellationToken ct = default);
    Task<List<UsageSnapshotDto>> GetUsageSnapshotsAsync(CancellationToken ct = default);
    Task<List<AuditSummaryDto>> GetRecentAuditsAsync(int take = 5, CancellationToken ct = default);
}

public sealed class DashboardService(HttpClient http) : IDashboardService
{
    private const string TenantStatusUrl = "/api/v1/tenants/me/status";
    private const string SubscriptionUrl = "/api/v1/billing/subscriptions/me";
    private const string UsageUrl = "/api/v1/billing/usage";
    private const string AuditsUrl = "/api/v1/audits";

    public async Task<TenantStatusDto> GetMyTenantStatusAsync(CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<TenantStatusDto>(TenantStatusUrl, ct);
        return response ?? throw new InvalidOperationException("Failed to retrieve tenant status");
    }

    public async Task<SubscriptionDto?> GetMySubscriptionAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<SubscriptionDto?>(SubscriptionUrl, ct);
    }

    public async Task<List<UsageSnapshotDto>> GetUsageSnapshotsAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<List<UsageSnapshotDto>>(UsageUrl, ct);
        return result ?? new List<UsageSnapshotDto>();
    }

    public async Task<List<AuditSummaryDto>> GetRecentAuditsAsync(int take = 5, CancellationToken ct = default)
    {
        // The audits endpoint always orders by OccurredAtUtc descending (handler
        // ignores Sort); pagination params are PageNumber/PageSize.
        var query = $"?PageNumber=1&PageSize={take}";
        var response = await http.GetFromJsonAsync<PagedResult<AuditSummaryDto>>($"{AuditsUrl}{query}", ct);
        return response?.Items ?? new List<AuditSummaryDto>();
    }
}