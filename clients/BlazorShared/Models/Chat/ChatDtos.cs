using System.Text.Json.Serialization;

namespace FSH.BlazorShared.Models.Chat;

// ─── Enums ────────────────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter<ChannelType>))]
public enum ChannelType { DirectMessage, GroupMessage, Channel }

[JsonConverter(typeof(JsonStringEnumConverter<ChannelMemberRole>))]
public enum ChannelMemberRole { Member, Admin }

// ─── Response DTOs ────────────────────────────────────────────────────────

public sealed record ChannelDto(
    Guid Id,
    ChannelType Type,
    string? Name,
    string? Slug,
    string? Description,
    bool IsPrivate,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastMessageAtUtc,
    int UnreadCount,
    IReadOnlyList<ChannelMemberDto> Members);

public sealed record ChannelMemberDto(
    Guid Id,
    string UserId,
    ChannelMemberRole Role,
    DateTime JoinedAtUtc,
    Guid? LastReadMessageId);

public sealed record MessageDto(
    Guid Id,
    Guid ChannelId,
    string AuthorUserId,
    string? Body,
    Guid? ParentMessageId,
    int ReplyCount,
    DateTime? EditedAtUtc,
    DateTime? DeletedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    IReadOnlyList<MessageReactionDto> Reactions,
    bool IsPinned = false,
    string? PinnedByUserId = null,
    DateTime? PinnedAtUtc = null);

public sealed record MessageAttachmentDto(
    Guid Id,
    Guid? FileAssetId,
    string Url,
    string ContentType,
    string OriginalFileName,
    long SizeBytes);

public sealed record MessageReactionDto(
    Guid Id,
    Guid MessageId,
    string UserId,
    string Emoji,
    DateTime CreatedAtUtc);

// ─── Request DTOs ─────────────────────────────────────────────────────────

public sealed record CreateChannelRequest(
    string Name,
    string? Description = null,
    bool IsPrivate = false);

public sealed record UpdateChannelRequest(
    string Name,
    string? Description = null,
    bool IsPrivate = false);

public sealed record SendMessageRequest(
    string? Body = null,
    Guid? ParentMessageId = null,
    IReadOnlyList<MessageAttachmentRequest>? Attachments = null);

public sealed record MessageAttachmentRequest(
    Guid? FileAssetId,
    string Url,
    string ContentType,
    string OriginalFileName,
    long SizeBytes);

public sealed record EditMessageRequest(string Body);

public sealed record AddReactionRequest(string Emoji);

public sealed record AddChannelMembersRequest(IReadOnlyList<string> UserIds);

public sealed record FindOrCreateDmRequest(IReadOnlyList<string> UserIds);

public sealed record MarkChannelReadRequest(Guid MessageId);
