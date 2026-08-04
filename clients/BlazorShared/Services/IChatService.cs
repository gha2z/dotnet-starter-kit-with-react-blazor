using FSH.BlazorShared.Models.Chat;

namespace FSH.BlazorShared.Services;

public interface IChatService
{
    // Channels
    Task<IReadOnlyList<ChannelDto>> ListMyChannelsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IReadOnlyList<ChannelDto>> DiscoverChannelsAsync(string? search = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<ChannelDto> GetChannelByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateChannelAsync(CreateChannelRequest request, CancellationToken ct = default);
    Task<Guid> FindOrCreateDmAsync(FindOrCreateDmRequest request, CancellationToken ct = default);
    Task UpdateChannelAsync(Guid id, UpdateChannelRequest request, CancellationToken ct = default);
    Task ArchiveChannelAsync(Guid id, CancellationToken ct = default);
    Task RestoreChannelAsync(Guid id, CancellationToken ct = default);
    Task AddChannelMembersAsync(Guid id, AddChannelMembersRequest request, CancellationToken ct = default);
    Task RemoveChannelMemberAsync(Guid id, string userId, CancellationToken ct = default);
    Task MarkChannelReadAsync(Guid id, MarkChannelReadRequest request, CancellationToken ct = default);

    // Messages
    Task<IReadOnlyList<MessageDto>> ListChannelMessagesAsync(Guid channelId, Guid? before = null, int pageSize = 50, CancellationToken ct = default);
    Task<IReadOnlyList<MessageDto>> ListMessageRepliesAsync(Guid messageId, Guid? before = null, int pageSize = 50, CancellationToken ct = default);
    Task<IReadOnlyList<MessageDto>> GetPinnedMessagesAsync(Guid channelId, CancellationToken ct = default);
    Task<MessageDto> SendMessageAsync(Guid channelId, SendMessageRequest request, CancellationToken ct = default);
    Task EditMessageAsync(Guid messageId, EditMessageRequest request, CancellationToken ct = default);
    Task DeleteMessageAsync(Guid messageId, CancellationToken ct = default);
    Task PinMessageAsync(Guid messageId, CancellationToken ct = default);
    Task UnpinMessageAsync(Guid messageId, CancellationToken ct = default);

    // Reactions
    Task AddReactionAsync(Guid messageId, AddReactionRequest request, CancellationToken ct = default);
    Task RemoveReactionAsync(Guid messageId, string emoji, CancellationToken ct = default);

    // Search
    Task<IReadOnlyList<MessageDto>> SearchMessagesAsync(string query, Guid? channelId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
}
