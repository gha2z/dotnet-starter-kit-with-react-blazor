using System.Net.Http.Json;
using FSH.BlazorShared.Models.Notifications;

namespace FSH.BlazorShared.Services;

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    Task<List<NotificationDto>> ListAsync(bool unreadOnly = false, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task MarkReadAsync(Guid id, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(CancellationToken ct = default);
}

public sealed class NotificationService(HttpClient http) : INotificationService
{
    private const string NotificationsBase = "/api/v1/notifications";

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<int>($"{NotificationsBase}/unread-count", ct);

    public async Task<List<NotificationDto>> ListAsync(bool unreadOnly = false, int page = 1, int pageSize = 50, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<NotificationDto>>(
            $"{NotificationsBase}?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}&page={page}&pageSize={pageSize}", ct) ?? [];

    public async Task MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.PostAsync($"{NotificationsBase}/{id}/read", content: null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> MarkAllReadAsync(CancellationToken ct = default)
    {
        using var response = await http.PostAsync($"{NotificationsBase}/read-all", content: null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MarkAllReadResponse>(cancellationToken: ct);
        return result?.Updated ?? 0;
    }

    private sealed record MarkAllReadResponse(int Updated);
}
