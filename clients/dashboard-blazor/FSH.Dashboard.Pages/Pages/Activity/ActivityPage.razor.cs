using System.Text.Json;
using FSH.BlazorShared.Sse;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Activity;

public sealed partial class ActivityPage : IDisposable
{
    private const int MaxEvents = 200;

    [Inject] private ISseService SseService { get; set; } = default!;

    private readonly List<ActivityEvent> _events = new();
    private int _eventCount;
    private bool _isConnected;
    private IDisposable? _subscription;
    private bool _disposed;

    // React parity: connection state drives the header badge and the empty state copy.
    private bool IsLive => _isConnected;

    protected override void OnInitialized()
    {
        _isConnected = SseService.IsConnected;
        _subscription = SseService.Messages.Subscribe(new SseObserver(OnEvent));
        SseService.ConnectionChanged += OnConnectionChanged;
    }

    private void OnEvent(SseEvent message)
    {
        _eventCount++;
        _events.Insert(0, new ActivityEvent(
            message.EventType,
            message.Data,
            message.Id,
            DateTime.Now));

        if (_events.Count > MaxEvents)
        {
            _events.RemoveAt(_events.Count - 1);
        }

        InvokeAsync(StateHasChanged);
    }

    private void OnConnectionChanged()
    {
        _isConnected = SseService.IsConnected;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _subscription?.Dispose();
        SseService.ConnectionChanged -= OnConnectionChanged;
    }

    /// <summary>
    /// React parity (activity.tsx eventTone): failures pop red, successes green,
    /// warnings amber, auth/token info blue, everything else neutral.
    /// </summary>
    private static Color EventTone(string type)
    {
        var t = type.ToLowerInvariant();
        if (t.Contains("fail") || t.Contains("error") || t.Contains("revoke")) return Color.Error;
        if (t.Contains("warn") || t.Contains("retry")) return Color.Warning;
        if (t.Contains("login") || t.Contains("issued") || t.Contains("created")) return Color.Success;
        if (t.Contains("token") || t.Contains("auth")) return Color.Info;
        return Color.Default;
    }

    /// <summary>
    /// React parity (payloadSummary): pass through a plain string payload, otherwise
    /// compact-serialize the JSON object, falling back to the raw data on parse failure.
    /// </summary>
    private static string PayloadSummary(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return data;
        }

        try
        {
            using var doc = JsonDocument.Parse(data);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return doc.RootElement.GetString() ?? data;
            }

            return doc.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return data;
        }
    }

    /// <summary>
    /// React parity (entityLabel): most domain events carry an aggregate id under a
    /// predictable field — pull the first match out of the payload.
    /// </summary>
    private static string EntityLabel(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "—";
            }

            foreach (var key in new[] { "entityId", "aggregateId", "id", "tenantId", "userId" })
            {
                if (doc.RootElement.TryGetProperty(key, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { Length: > 0 } text)
                {
                    return text;
                }
            }
        }
        catch (JsonException)
        {
            // fall through to the neutral label
        }

        return "—";
    }

    /// <summary>Received-at is captured client-side (React parity: Date.now() at subscribe time).</summary>
    private static string FormatTime(DateTime receivedAt) => receivedAt.ToString("HH:mm:ss");

    private sealed class SseObserver(Action<SseEvent> onNext) : IObserver<SseEvent>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(SseEvent value) => onNext(value);
    }

    private sealed record ActivityEvent(string EventType, string Data, string? Id, DateTime ReceivedAt);
}
