using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Tickets;

namespace FSH.BlazorShared.Services;

public sealed class TicketService(HttpClient http) : ITicketService
{
    private const string Base = "/api/v1/tickets";

    private static string QueryString(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        return string.Join("&", parts);
    }

    public async Task<PagedResult<TicketDto>> SearchTicketsAsync(
        string? search = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? assignedToUserId = null,
        Guid? reporterUserId = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken ct = default)
    {
        var query = QueryString(
            ("search", search),
            ("status", status?.ToString()),
            ("priority", priority?.ToString()),
            ("assignedToUserId", assignedToUserId?.ToString()),
            ("reporterUserId", reporterUserId?.ToString()),
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("sortBy", sortBy),
            ("sortDir", sortDir));

        return await http.GetFromJsonAsync<PagedResult<TicketDto>>($"{Base}?{query}", ct)
               ?? new PagedResult<TicketDto>([], pageNumber, pageSize, 0, 0, false, false);
    }

    public async Task<TicketDto> GetTicketByIdAsync(Guid id, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<TicketDto>($"{Base}/{id}", ct)
        ?? throw new InvalidOperationException("Ticket not found.");

    public async Task<Guid> CreateTicketAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(Base, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> AssignTicketAsync(Guid ticketId, AssignTicketRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/{ticketId}/assign", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> ResolveTicketAsync(Guid ticketId, ResolveTicketRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/{ticketId}/resolve", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> ReopenTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{Base}/{ticketId}/reopen", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<IReadOnlyList<TicketCommentDto>> ListTicketCommentsAsync(Guid ticketId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<TicketCommentDto>>($"{Base}/{ticketId}/comments", ct)
        ?? [];

    public async Task<Guid> AddTicketCommentAsync(Guid ticketId, AddTicketCommentRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/{ticketId}/comments", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<PagedResult<TicketDto>> ListTrashedTicketsAsync(int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = QueryString(("pageNumber", pageNumber.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<PagedResult<TicketDto>>($"{Base}/tickets/trash?{query}", ct)
            ?? new PagedResult<TicketDto>([], pageNumber, pageSize, 0, 0, false, false);
    }

    public async Task RestoreTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{Base}/tickets/{ticketId}/restore", null, ct);
        response.EnsureSuccessStatusCode();
    }
}
