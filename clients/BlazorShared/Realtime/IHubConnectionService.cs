using Microsoft.AspNetCore.SignalR.Client;

namespace FSH.BlazorShared.Realtime;

public interface IHubConnectionService
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    IDisposable On<T>(string eventName, Func<T, Task> handler);
    Task SendAsync(string methodName, object? arg, CancellationToken ct = default);
    event Action<HubConnectionState>? StateChanged;
}
