using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Tickets;

namespace FSH.BlazorShared.Services;

public interface ITicketService
{
    Task<PagedResult<TicketDto>> SearchTicketsAsync(
        string? search = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? assignedToUserId = null,
        Guid? reporterUserId = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default);

    Task<TicketDto> GetTicketByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateTicketAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task<Guid> AssignTicketAsync(Guid ticketId, AssignTicketRequest request, CancellationToken ct = default);
    Task<Guid> ResolveTicketAsync(Guid ticketId, ResolveTicketRequest request, CancellationToken ct = default);
    Task<Guid> ReopenTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<TicketCommentDto>> ListTicketCommentsAsync(Guid ticketId, CancellationToken ct = default);
    Task<Guid> AddTicketCommentAsync(Guid ticketId, AddTicketCommentRequest request, CancellationToken ct = default);
}
