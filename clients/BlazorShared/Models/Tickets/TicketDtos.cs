using System.Text.Json.Serialization;

namespace FSH.BlazorShared.Models.Tickets;

// ─── Enums ────────────────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter<TicketStatus>))]
public enum TicketStatus { Open, InProgress, Resolved, Closed }

[JsonConverter(typeof(JsonStringEnumConverter<TicketPriority>))]
public enum TicketPriority { Low, Medium, High, Critical }

// ─── Response DTOs ────────────────────────────────────────────────────────

public sealed record TicketDto(
    Guid Id,
    string Number,
    string Title,
    string? Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid ReporterUserId,
    Guid? AssignedToUserId,
    string? ResolutionNote,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ResolvedAtUtc,
    DateTime? ClosedAtUtc,
    int CommentCount);

public sealed record TicketCommentDto(
    Guid Id,
    Guid TicketId,
    Guid AuthorUserId,
    string Body,
    DateTime CreatedAtUtc);

// ─── Request DTOs ─────────────────────────────────────────────────────────

public sealed record CreateTicketRequest(
    string Title,
    string? Description = null,
    TicketPriority Priority = TicketPriority.Medium,
    Guid? AssignedToUserId = null);

public sealed record AssignTicketRequest(Guid? AssigneeUserId);

public sealed record ResolveTicketRequest(string? ResolutionNote);

public sealed record AddTicketCommentRequest(string Body);
