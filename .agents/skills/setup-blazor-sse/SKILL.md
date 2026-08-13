---
name: setup-blazor-sse
description: Wire Server-Sent Events (SSE) streaming for the dashboard Blazor app. Use only in the dashboard-blazor project. See .agents/rules/frontend/blazor-dashboard.md.
argument-hint: "dashboard"
---

# Setup Blazor SSE

SSE is used only in the **dashboard** app for the Overview page's real-time activity feed.

The server exposes a **two-step token flow** (browsers' `EventSource` cannot send an `Authorization` header):

1. `POST /api/v1/sse/token` (JWT-authenticated) → `{ token }` — opaque, single-use, 30s TTL.
2. `GET /api/v1/sse/stream?token={guid}` (anonymous endpoint, consumes the token) → `text/event-stream`.

Do **not** use `/api/v1/realtime/stream` — that endpoint does not exist (404). See `.agents/rules/realtime.md`.

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

`BlazorShared/Sse/SseService.cs` (`SseService` + `SseTokenResponse` record). Key behaviors:

- `StartAsync` launches a background **connect loop** (does not block): token exchange → stream connect → read until drop → retry with capped backoff (1s → 2s → … → 30s, reset on success).
- Each attempt re-does the token exchange (opaque tokens are single-use), so reconnects self-heal after JWT expiry or server restarts.
- `StopAsync`/`DisposeAsync` cancel the loop; `ReadStreamAsync` swallows `OperationCanceledException` on cancellation (no unobserved task exceptions).
- The `FSH.Api` HttpClient already carries the `AuthDelegatingHandler` (Bearer + tenant) — the explicit `Authorization` header on the token exchange is belt-and-suspenders.

```csharp
public sealed class SseService(HttpClient http, ITokenStore tokenStore, ILogger<SseService> logger) : ISseService, IAsyncDisposable
{
    private readonly Subject<SseEvent> _subject = new();
    private CancellationTokenSource? _cts;
    private TimeSpan _retryDelay = TimeSpan.FromSeconds(1);
    private const string TokenUrl = "/api/v1/sse/token";
    private const string StreamUrl = "/api/v1/sse/stream";

    public IObservable<SseEvent> Messages => _subject;
    public bool IsConnected => _cts is not null && !_cts.IsCancellationRequested;

    public Task StartAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = Task.Run(() => ConnectLoopAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task ConnectLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectOnceAsync(ct);
                logger.LogWarning("SSE stream ended; reconnecting in {Delay}", _retryDelay);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "SSE connection failed; retrying in {Delay}", _retryDelay);
            }

            try { await Task.Delay(_retryDelay, ct); }
            catch (OperationCanceledException) { break; }

            _retryDelay = TimeSpan.FromSeconds(Math.Min(_retryDelay.TotalSeconds * 2, 30));
        }
    }

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        var token = await tokenStore.GetAccessTokenAsync();
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        if (token is not null)
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tokenResponse = await http.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenDto = await tokenResponse.Content.ReadFromJsonAsync<SseTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("SSE token exchange returned no token.");

        var streamRequest = new HttpRequestMessage(HttpMethod.Get, $"{StreamUrl}?token={Uri.EscapeDataString(tokenDto.Token)}");
        var streamResponse = await http.SendAsync(streamRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        streamResponse.EnsureSuccessStatusCode();

        var stream = await streamResponse.Content.ReadAsStreamAsync(ct);
        _retryDelay = TimeSpan.FromSeconds(1);
        logger.LogInformation("SSE connected");
        await ReadStreamAsync(stream, ct);
    }

    private async Task ReadStreamAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        string? eventType = null;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;
                if (line.StartsWith("event: ")) eventType = line[7..];
                else if (line.StartsWith("data: ")) _subject.OnNext(new SseEvent(eventType ?? "message", line[6..]));
                else if (string.IsNullOrEmpty(line)) eventType = null;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _subject.Dispose();
        await Task.CompletedTask;
    }
}
```

## Step 3 — Wire in dashboard

```csharp
// FSH.Dashboard.Wasm/Program.cs — scoped, resolves the FSH.Api authed client
builder.Services.AddScoped<ISseService, SseService>();
```

The **lifecycle is auth-driven** — never connect unconditionally, and never connect while signed out (401 noise):

```csharp
// FSH.Dashboard.Wasm/App.razor.cs — subscribe once, gate on the token
private bool _sseStarting;

protected override async Task OnInitializedAsync()
{
    TokenStore.TokensChanged += OnTokensChanged;
    _sseSub = Sse.Messages.Subscribe(new SseObserver(OnSseEvent));
    try { await EnsureSseConnectionAsync(); }
    catch (Exception ex) { Logger.LogDebug(ex, "SSE connection failed at startup; will retry after login"); }
}

private void OnTokensChanged()
{
    InvokeAsync(async () =>
    {
        StateHasChanged();
        if (await TokenStore.GetAccessTokenAsync() is null)
        {
            await Sse.StopAsync();   // logout — kill the stream, no reconnect
            return;
        }
        try { await EnsureSseConnectionAsync(); }
        catch (Exception ex) { Logger.LogDebug(ex, "SSE connection failed after login; will retry on next token change"); }
    });
}

private async Task EnsureSseConnectionAsync()
{
    if (_sseStarting || Sse.IsConnected) return;
    if (await TokenStore.GetAccessTokenAsync() is null) return;   // signed out — no attempt, no 401
    _sseStarting = true;
    try { await Sse.StartAsync(); }
    finally { _sseStarting = false; }
}
```

## Validation

- [ ] No `POST /api/v1/sse/token` calls at all while signed out (startup + after logout)
- [ ] Login → token exchange 200 → stream connects (`SSE connected` log)
- [ ] Logout → stream stopped, no further requests
- [ ] API restart while connected → reconnects with backoff (1s → … → 30s)
- [ ] Stream events reach `Messages` subscribers; disposal cancels everything
- [ ] Works alongside SignalR hub (separate connections)
