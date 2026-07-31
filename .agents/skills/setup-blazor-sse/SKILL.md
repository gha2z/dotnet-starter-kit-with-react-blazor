---
name: setup-blazor-sse
description: Wire Server-Sent Events (SSE) streaming for the dashboard Blazor app. Use only in the dashboard-blazor project. See .agents/rules/frontend/blazor-dashboard.md.
argument-hint: "dashboard"
---

# Setup Blazor SSE

SSE is used only in the **dashboard** app for the Overview page's real-time activity feed.

## Step 1 — SSE service

```csharp
// BlazorShared/Sse/ISseService.cs
public interface ISseService
{
    IObservable<SseEvent> Messages { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    bool IsConnected { get; }
}

public record SseEvent(string EventType, string Data, string? Id = null);
```

## Step 2 — Implementation

```csharp
// BlazorShared/Sse/SseService.cs
public sealed class SseService(HttpClient http, ITokenStore tokenStore) : ISseService, IAsyncDisposable
{
    private readonly Subject<SseEvent> _subject = new();
    private CancellationTokenSource? _cts;
    private const string SseUrl = "/api/v1/realtime/stream";

    public IObservable<SseEvent> Messages => _subject;

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var request = new HttpRequestMessage(HttpMethod.Get, SseUrl);
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
        _ = Task.Run(() => ReadStreamAsync(stream, _cts.Token), _cts.Token);
    }

    private async Task ReadStreamAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        string? eventType = null;
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (line.StartsWith("event: "))
                eventType = line[7..];
            else if (line.StartsWith("data: "))
                _subject.OnNext(new SseEvent(eventType ?? "message", line[6..]));
            else if (string.IsNullOrEmpty(line))
                eventType = null; // empty line = event delimiter
        }
    }
}
```

## Step 3 — Wire in dashboard

```csharp
// FSH.Dashboard.Wasm/Program.cs
builder.Services.AddSingleton<SseService>();

// FSH.Dashboard.Wasm/App.razor
@inject SseService Sse

protected override async Task OnInitializedAsync()
{
    _sseSubscription = Sse.Messages.Subscribe(msg => HandleSseMessage(msg));
    await Sse.StartAsync();
}
```

## Validation

- [ ] SSE connects on app start after authentication
- [ ] Activity feed updates in real-time without polling
- [ ] Reconnection on connection loss with exponential backoff
- [ ] Disposal: subscription removed on component disposal
- [ ] Works alongside SignalR hub (separate connections)
