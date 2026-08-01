using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;

namespace FSH.BlazorShared.Services;

public interface IBillingService
{
    Task<List<BillingPlanDto>> GetPlansAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
}
