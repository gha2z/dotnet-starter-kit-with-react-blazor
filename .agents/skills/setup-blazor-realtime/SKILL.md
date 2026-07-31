---
name: setup-blazor-realtime
description: Wire SignalR hub connection for real-time notifications and chat in Blazor WASM. Use when adding real-time features. See .agents/rules/frontend/blazor-shared.md.
argument-hint: "[admin|dashboard]"
---

# Setup Blazor Realtime (SignalR)

## Step 1 — Hub connection service

```csharp
// BlazorShared/Realtime/IHubConnectionService.cs
public interface IHubConnectionService
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    IDisposable On<T>(string eventName, Func<T, Task> handler);
    Task SendAsync(string methodName, object? arg, CancellationToken ct = default);
    event Action<HubConnectionState>? StateChanged;
}

// BlazorShared/Realtime/HubConnectionService.cs
public sealed class HubConnectionService(ITokenStore tokenStore, NavigationManager nav) : IHubConnectionService, IAsyncDisposable
{
    private HubConnection? _hub;

    public HubConnectionState State => _hub?.State ?? HubConnectionState.Disconnected;

    public async Task StartAsync(CancellationToken ct = default)
    {
        _hub = new HubConnectionBuilder()
            .WithUrl(nav.ToAbsoluteUri("/api/v1/realtime/hub"), options =>
            {
                options.AccessTokenProvider = async () => await tokenStore.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect(new RetryPolicy())
            .Build();

        _hub.Closed += async (error) => { StateChanged?.Invoke(HubConnectionState.Disconnected); await Task.CompletedTask; };
        _hub.Reconnected += async (id) => { StateChanged?.Invoke(HubConnectionState.Connected); await Task.CompletedTask; };

        await _hub.StartAsync(ct);
        StateChanged?.Invoke(HubConnectionState.Connected);
    }

    public IDisposable On<T>(string eventName, Func<T, Task> handler)
        => _hub?.On(eventName, handler) ?? Disposable.Empty;

    public async Task SendAsync(string methodName, object? arg, CancellationToken ct = default)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.SendAsync(methodName, arg, ct);
    }
}
```

## Step 2 — Wire in App.razor

```csharp
// Admin app — Notifications only
@inject IHubConnectionService Hub

protected override async Task OnInitializedAsync()
{
    await Hub.StartAsync();
    _notificationSub = Hub.On<Notification>("NotificationCreated", async n =>
    {
        Snackbar.Add(n.Message, Severity.Info);
        await InvokeAsync(StateHasChanged);
    });
}
```

## Step 3 — Chat (dashboard app)

For the Chat page with real-time messaging, see `Pages/Chat/ChatPage.razor` — it subscribes to `MessageReceived` events and sends via `SendAsync("SendMessage", payload)`.

## Validation

- [ ] Hub connects on app start when authenticated
- [ ] Hub disconnects on logout
- [ ] Hub reconnects on token refresh
- [ ] Notifications appear as toasts in real-time
- [ ] Chat messages appear without page refresh
- [ ] Disposal: `IDisposable` unsubscribes on page navigation
