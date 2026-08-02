using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;

namespace FSH.BlazorShared.Services;

public sealed class BillingService(HttpClient http) : IBillingService
{
    private const string BillingBase = "/api/v1/billing";

    public async Task<List<BillingPlanDto>> GetPlansAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<BillingPlanDto>>(
            $"{BillingBase}/plans?includeInactive={includeInactive.ToString().ToLowerInvariant()}", ct) ?? [];
    }

    public async Task<Guid> CreatePlanAsync(CreatePlanRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{BillingBase}/plans", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> UpdatePlanAsync(Guid planId, UpdatePlanRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{BillingBase}/plans/{planId}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? tenantId = null,
        string? status = null,
        int? periodYear = null,
        int? periodMonth = null,
        CancellationToken ct = default)
    {
        var query = QueryString(
            pageNumber, pageSize,
            ("tenantId", tenantId),
            ("status", status),
            ("periodYear", periodYear?.ToString()),
            ("periodMonth", periodMonth?.ToString()));
        return await http.GetFromJsonAsync<PagedResult<InvoiceDto>>($"{BillingBase}/invoices?{query}", ct)
            ?? EmptyPage<InvoiceDto>(pageSize);
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<InvoiceDto>($"{BillingBase}/invoices/{id}", ct)
            ?? throw new InvalidOperationException("Null invoice response");
    }

    public async Task<byte[]> GetInvoicePdfAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"{BillingBase}/invoices/{id}/pdf", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task IssueInvoiceAsync(Guid id, DateTime? dueAtUtc, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{BillingBase}/invoices/{id}/issue", new { dueAtUtc }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkInvoicePaidAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{BillingBase}/invoices/{id}/pay", content: null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task VoidInvoiceAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{BillingBase}/invoices/{id}/void", new { reason }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResult<TopupRequestDto>> GetTopupRequestsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? tenantId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var query = QueryString(
            pageNumber, pageSize,
            ("tenantId", tenantId),
            ("status", status));
        return await http.GetFromJsonAsync<PagedResult<TopupRequestDto>>($"{BillingBase}/wallet/topup-requests?{query}", ct)
            ?? EmptyPage<TopupRequestDto>(pageSize);
    }

    public async Task<Guid> ApproveTopupRequestAsync(Guid id, string? note, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{BillingBase}/wallet/topup-requests/{id}/approve", new { note }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> RejectTopupRequestAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{BillingBase}/wallet/topup-requests/{id}/reject", new { reason }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    private static string QueryString(int pageNumber, int pageSize, params (string Key, string? Value)[] parameters)
    {
        var parts = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        parts.AddRange(parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}"));
        return string.Join("&", parts);
    }

    private static PagedResult<T> EmptyPage<T>(int pageSize) =>
        new([], 1, pageSize, 0, 0, false, false);
}
