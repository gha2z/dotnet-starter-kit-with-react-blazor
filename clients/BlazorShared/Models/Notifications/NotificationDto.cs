namespace FSH.BlazorShared.Models.Notifications;

/// <summary>
/// Mirrors the server's <c>NotificationDto</c> (and the SignalR "NotificationCreated"
/// payload — missing JSON members deserialize to defaults, so the hub event feeds
/// straight into this type). System.Text.Json web defaults are case-insensitive.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? Link,
    string Source,
    string MetadataJson,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc)
{
    public bool IsRead => ReadAtUtc is not null;
}
