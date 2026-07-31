using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FSH.BlazorShared.Infrastructure;

public interface IInactivityTimerService
{
    event Action? OnTimeout;
    event Action? OnWarning;
    void Start();
    void Stop();
    void Reset();
    bool IsWarningShown { get; }
    void DismissWarning();
}

public sealed class InactivityTimerService(
    IJSRuntime js,
    ILogger<InactivityTimerService> logger) : IInactivityTimerService, IAsyncDisposable
{
    private const int WarningSeconds = 60;
    private Timer? _timer;
    private int _remainingSeconds;
    private bool _warningShown;

    public event Action? OnTimeout;
    public event Action? OnWarning;
    public bool IsWarningShown => _warningShown;

    public void Start()
    {
        Stop();
        _remainingSeconds = 10 * 60; // 10 minutes default
        _warningShown = false;
        _timer = new Timer(Tick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        _ = ListenForActivityAsync();
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Reset()
    {
        _remainingSeconds = 10 * 60;
        _warningShown = false;
    }

    public void DismissWarning()
    {
        _warningShown = false;
        Reset();
    }

    private void Tick(object? state)
    {
        _remainingSeconds--;

        if (_remainingSeconds <= WarningSeconds && !_warningShown)
        {
            _warningShown = true;
            OnWarning?.Invoke();
        }

        if (_remainingSeconds <= 0)
        {
            Stop();
            OnTimeout?.Invoke();
        }
    }

    private async Task ListenForActivityAsync()
    {
        try
        {
            var script = @"
                (function() {
                    let timer;
                    const reset = () => {
                        if (timer) clearTimeout(timer);
                        timer = setTimeout(() => DotNet.invokeMethodAsync('FSH.BlazorShared', 'OnUserActivity'), 100);
                    };
                    ['click', 'keydown', 'mousemove', 'touchstart', 'scroll'].forEach(e => window.addEventListener(e, reset));
                    reset();
                })();
            ";
            await js.InvokeVoidAsync("eval", script);
        }
        catch
        {
            logger.LogWarning("Activity listener not available");
        }
    }

    [JSInvokable]
    public static void OnUserActivity()
    {
        // Accessed via the JS invokable — calls Reset on the stored instance
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        await Task.CompletedTask;
    }
}
