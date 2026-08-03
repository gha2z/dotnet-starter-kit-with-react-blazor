using System.Net.Http.Headers;
using System.Net.Http.Json;
using FSH.BlazorShared.Auth;
using Microsoft.Extensions.Logging;

namespace FSH.BlazorShared.Sse;

public sealed class SseService(HttpClient http, ITokenStore tokenStore, ILogger<SseService> logger) : ISseService, IAsyncDisposable
{
    private readonly Subject<SseEvent> _subject = new();
    private CancellationTokenSource? _cts;
    private TimeSpan _retryDelay = TimeSpan.FromSeconds(1);
    private const string TokenUrl = "/api/v1/sse/token";
    private const string StreamUrl = "/api/v1/sse/stream";

    public IObservable<SseEvent> Messages => _subject;
    public bool IsConnected => _cts is not null && !_cts.IsCancellationRequested;
    public event Action? ConnectionChanged;

    public Task StartAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = Task.Run(() => ConnectLoopAsync(_cts.Token), CancellationToken.None);
        ConnectionChanged?.Invoke();
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
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "SSE connection failed; retrying in {Delay}", _retryDelay);

                // The auth layer clears tokens and flips the app back to login when a
                // session dies (refresh failure). Stop retrying instead of hammering a
                // dead session forever — the App root restarts us after the next login.
                if (await tokenStore.GetAccessTokenAsync() is null)
                {
                    logger.LogInformation("SSE stopped: no session token");
                    break;
                }
            }

            ConnectionChanged?.Invoke();

            try
            {
                await Task.Delay(_retryDelay, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _retryDelay = TimeSpan.FromSeconds(Math.Min(_retryDelay.TotalSeconds * 2, 30));
        }
    }

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        var token = await tokenStore.GetAccessTokenAsync();

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        if (token is not null)
        {
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var tokenResponse = await http.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenDto = await tokenResponse.Content.ReadFromJsonAsync<SseTokenResponse>(cancellationToken: ct);
        if (tokenDto?.Token is not { Length: > 0 })
        {
            throw new InvalidOperationException("SSE token exchange returned an empty token.");
        }

        var streamRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{StreamUrl}?token={Uri.EscapeDataString(tokenDto.Token)}");
        var streamResponse = await http.SendAsync(streamRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        streamResponse.EnsureSuccessStatusCode();

        var stream = await streamResponse.Content.ReadAsStreamAsync(ct);
        _retryDelay = TimeSpan.FromSeconds(1);
        logger.LogInformation("SSE connected");
        ConnectionChanged?.Invoke();
        await ReadStreamAsync(stream, ct);
        ct.ThrowIfCancellationRequested();
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        ConnectionChanged?.Invoke();
        return Task.CompletedTask;
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

                if (line.StartsWith("event: ", StringComparison.Ordinal))
                    eventType = line[7..];
                else if (line.StartsWith("data: ", StringComparison.Ordinal))
                    _subject.OnNext(new SseEvent(eventType ?? "message", line[6..]));
                else if (string.IsNullOrEmpty(line))
                    eventType = null;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Stopped via StopAsync/DisposeAsync — expected.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _subject.Dispose();
        await Task.CompletedTask;
    }
}

internal sealed class Subject<T> : IObservable<T>, IDisposable
{
    private readonly List<IObserver<T>> _observers = [];
    private readonly object _lock = new();

    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_lock) _observers.Add(observer);
        return new Subscription(() => { lock (_lock) _observers.Remove(observer); });
    }

    public void OnNext(T value)
    {
        lock (_lock)
        {
            foreach (var o in _observers) o.OnNext(value);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var o in _observers) o.OnCompleted();
            _observers.Clear();
        }
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
