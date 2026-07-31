namespace FSH.BlazorShared.Sse;

public interface ISseService
{
    IObservable<SseEvent> Messages { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    bool IsConnected { get; }
}

public sealed record SseEvent(string EventType, string Data, string? Id = null);
