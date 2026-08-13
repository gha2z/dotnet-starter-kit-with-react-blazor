namespace FSH.BlazorShared.Sse;

public interface ISseService
{
    IObservable<SseEvent> Messages { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    bool IsConnected { get; }

    /// <summary>
    /// Raised whenever the connection state may have changed (start, established,
    /// stream ended / reconnect scheduled, stopped). Handlers run on the SSE loop
    /// thread — marshal to the UI with <c>InvokeAsync(StateHasChanged)</c>.
    /// </summary>
    event Action? ConnectionChanged;
}

public sealed record SseEvent(string EventType, string Data, string? Id = null);
