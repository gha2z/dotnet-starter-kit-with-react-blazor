using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;

namespace FSH.BlazorShared.Services;

public interface IBillingService
{
    Task<List<BillingPlanDto>> GetPlansAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Guid> CreatePlanAsync(CreatePlanRequest request, CancellationToken ct = default);
    Task<Guid> UpdatePlanAsync(Guid planId, UpdatePlanRequest request, CancellationToken ct = default);
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? tenantId = null,
        string? status = null,
        int? periodYear = null,
        int? periodMonth = null,
        CancellationToken ct = default);
    Task<InvoiceDto> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetInvoicePdfAsync(Guid id, CancellationToken ct = default);
    Task IssueInvoiceAsync(Guid id, DateTime? dueAtUtc, CancellationToken ct = default);
    Task MarkInvoicePaidAsync(Guid id, CancellationToken ct = default);
    Task VoidInvoiceAsync(Guid id, string? reason, CancellationToken ct = default);
    Task<PagedResult<TopupRequestDto>> GetTopupRequestsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? tenantId = null,
        string? status = null,
        CancellationToken ct = default);
    Task<Guid> ApproveTopupRequestAsync(Guid id, string? note, CancellationToken ct = default);
    Task<Guid> RejectTopupRequestAsync(Guid id, string? reason, CancellationToken ct = default);
}
