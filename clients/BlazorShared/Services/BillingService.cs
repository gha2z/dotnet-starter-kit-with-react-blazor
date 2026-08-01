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

    public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<PagedResult<InvoiceDto>>(
            $"{BillingBase}/invoices?pageNumber={pageNumber}&pageSize={pageSize}", ct) ?? new PagedResult<InvoiceDto>([], 1, pageSize, 0, 0, false, false);
    }
}
