using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace FSH.BlazorShared.Realtime;

public sealed class HubConnectionService(
    ITokenStore tokenStore,
    NavigationManager nav,
    IRuntimeConfigService config,
    ILogger<HubConnectionService> logger) : IHubConnectionService, IAsyncDisposable
{
    private HubConnection? _hub;

    public HubConnectionState State => _hub?.State ?? HubConnectionState.Disconnected;
    public event Action<HubConnectionState>? StateChanged;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_hub is not null)
        {
            await StopAsync();
        }

        var hubUri = new Uri(
            RuntimeConfigService.ResolveApiBase(nav.BaseUri, config.ApiBaseUrl),
            "/api/v1/realtime/hub");

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.AccessTokenProvider = () => tokenStore.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.Closed += OnClosed;
        _hub.Reconnected += OnReconnected;

        await _hub.StartAsync(ct);
        logger.LogInformation("SignalR hub connected");
        StateChanged?.Invoke(HubConnectionState.Connected);
    }

    public Task StopAsync()
    {
        if (_hub is null) return Task.CompletedTask;
        return _hub.StopAsync();
    }

    public IDisposable On<T>(string eventName, Func<T, Task> handler)
    {
        return _hub?.On(eventName, handler) ?? new NullDisposable();
    }

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }

    public async Task SendAsync(string methodName, object? arg, CancellationToken ct = default)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync(methodName, arg, ct);
        }
    }

    private Task OnClosed(Exception? exception)
    {
        logger.LogWarning(exception, "SignalR hub closed");
        StateChanged?.Invoke(HubConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        logger.LogInformation("SignalR hub reconnected as {ConnectionId}", connectionId);
        StateChanged?.Invoke(HubConnectionState.Connected);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            _hub.Closed -= OnClosed;
            _hub.Reconnected -= OnReconnected;
            await _hub.DisposeAsync();
        }
    }
}
