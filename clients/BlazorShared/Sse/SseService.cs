using System.Net.Http.Headers;
using FSH.BlazorShared.Auth;
using Microsoft.Extensions.Logging;

namespace FSH.BlazorShared.Sse;

public sealed class SseService(HttpClient http, ITokenStore tokenStore, ILogger<SseService> logger) : ISseService, IAsyncDisposable
{
    private readonly Subject<SseEvent> _subject = new();
    private CancellationTokenSource? _cts;
    private const string SseUrl = "/api/v1/realtime/stream";

    public IObservable<SseEvent> Messages => _subject;
    public bool IsConnected => _cts is not null && !_cts.IsCancellationRequested;

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var request = new HttpRequestMessage(HttpMethod.Get, SseUrl);
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
            _ = Task.Run(() => ReadStreamAsync(stream, _cts.Token), _cts.Token);
            logger.LogInformation("SSE connected");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SSE connection failed");
            throw;
        }
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    private async Task ReadStreamAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        string? eventType = null;

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
