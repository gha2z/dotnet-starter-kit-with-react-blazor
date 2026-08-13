using System.Net.Http.Json;
using FSH.BlazorShared.Models.Chat;

namespace FSH.BlazorShared.Services;

public sealed class ChatService(HttpClient http) : IChatService
{
    private const string Base = "/api/v1/chat";

    private static string QueryString(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        return string.Join("&", parts);
    }

    // ─── Channels ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ChannelDto>> ListMyChannelsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = QueryString(("page", page.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<ChannelDto>>($"{Base}/channels?{query}", ct) ?? [];
    }

    public async Task<IReadOnlyList<ChannelDto>> DiscoverChannelsAsync(string? search = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = QueryString(("search", search), ("page", page.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<ChannelDto>>($"{Base}/channels/discover?{query}", ct) ?? [];
    }

    public async Task<ChannelDto> GetChannelByIdAsync(Guid id, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<ChannelDto>($"{Base}/channels/{id}", ct)
        ?? throw new InvalidOperationException("Channel not found.");

    public async Task<Guid> CreateChannelAsync(CreateChannelRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/channels", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Guid> FindOrCreateDmAsync(FindOrCreateDmRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/dms", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task UpdateChannelAsync(Guid id, UpdateChannelRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{Base}/channels/{id}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ArchiveChannelAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{Base}/channels/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RestoreChannelAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{Base}/channels/{id}/restore", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task AddChannelMembersAsync(Guid id, AddChannelMembersRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/channels/{id}/members", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveChannelMemberAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{Base}/channels/{id}/members/{Uri.EscapeDataString(userId)}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkChannelReadAsync(Guid id, MarkChannelReadRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/channels/{id}/read", request, ct);
        response.EnsureSuccessStatusCode();
    }

    // ─── Messages ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MessageDto>> ListChannelMessagesAsync(Guid channelId, Guid? before = null, int pageSize = 50, CancellationToken ct = default)
    {
        var query = QueryString(("before", before?.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<MessageDto>>($"{Base}/channels/{channelId}/messages?{query}", ct) ?? [];
    }

    public async Task<IReadOnlyList<MessageDto>> ListMessageRepliesAsync(Guid messageId, Guid? before = null, int pageSize = 50, CancellationToken ct = default)
    {
        var query = QueryString(("before", before?.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<MessageDto>>($"{Base}/messages/{messageId}/replies?{query}", ct) ?? [];
    }

    public async Task<IReadOnlyList<MessageDto>> GetPinnedMessagesAsync(Guid channelId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<MessageDto>>($"{Base}/channels/{channelId}/pinned", ct) ?? [];

    public async Task<MessageDto> SendMessageAsync(Guid channelId, SendMessageRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/channels/{channelId}/messages", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageDto>(ct)
               ?? throw new InvalidOperationException("Failed to deserialize sent message.");
    }

    public async Task EditMessageAsync(Guid messageId, EditMessageRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{Base}/messages/{messageId}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{Base}/messages/{messageId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task PinMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{Base}/messages/{messageId}/pin", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnpinMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{Base}/messages/{messageId}/pin", ct);
        response.EnsureSuccessStatusCode();
    }

    // ─── Reactions ─────────────────────────────────────────────────────

    public async Task AddReactionAsync(Guid messageId, AddReactionRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/messages/{messageId}/reactions", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveReactionAsync(Guid messageId, string emoji, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{Base}/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ─── Search ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MessageDto>> SearchMessagesAsync(string query, Guid? channelId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var qs = QueryString(("q", query), ("channelId", channelId?.ToString()), ("page", page.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<MessageDto>>($"{Base}/search?{qs}", ct) ?? [];
    }
}
