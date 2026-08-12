using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;

namespace FSH.BlazorShared.Services;

public interface IAuditService
{
    Task<PagedResult<AuditSummaryDto>> ListAsync(ListAuditsRequest request, CancellationToken ct = default);
    Task<AuditDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<AuditSummaryAggregateDto> GetSummaryAsync(string? tenantId = null, CancellationToken ct = default);
    Task<IReadOnlyList<AuditSummaryDto>> GetByCorrelationAsync(
        string correlationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default);
}
