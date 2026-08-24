using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FSH.BlazorShared.Infrastructure;

public interface IInactivityTimerService
{
    event Action? OnTimeout;
    event Action? OnWarning;
    event Action? OnTick;
    void Start();
    void Stop();
    void Reset();
    bool IsWarningShown { get; }
    int SecondsLeft { get; }
    int WarningTotalSeconds { get; }
    void DismissWarning();
}

/// <summary>
/// Inactivity auto-logout (React parity: auth/inactivity.ts + use-inactivity-timeout.ts).
/// Ticks once per second while started; raises OnWarning when the final window opens
/// and OnTimeout when the session must sign out. User activity (any tab of this
/// document) resets the countdown via the JS listener.
/// </summary>
public sealed class InactivityTimerService(
    IJSRuntime js,
    IRuntimeConfigService config,
    ILogger<InactivityTimerService> logger) : IInactivityTimerService, IAsyncDisposable
{
    private const int WarningSeconds = 60;
    private Timer? _timer;
    private int _remainingSeconds;
    private bool _warningShown;

    // The service is a singleton — the JS-invokable static reset needs a handle
    // back to the live instance (static methods cannot see DI).
    private static InactivityTimerService? _live;

    public event Action? OnTimeout;
    public event Action? OnWarning;
    public event Action? OnTick;

    public bool IsWarningShown => _warningShown;
    public int SecondsLeft => _remainingSeconds;
    public int WarningTotalSeconds => WarningSeconds;

    private int IdleSeconds => Math.Max(60, config.InactivityTimeoutMinutes * 60);

    public void Start()
    {
        Stop();
        _remainingSeconds = IdleSeconds;
        _warningShown = false;
        _live = this;
        _timer = new Timer(Tick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        _ = ListenForActivityAsync();
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _warningShown = false;
        if (_live == this)
        {
            _live = null;
        }
    }

    public void Reset()
    {
        _remainingSeconds = IdleSeconds;
        _warningShown = false;
    }

    public void DismissWarning()
    {
        Reset();
    }

    private void Tick(object? state)
    {
        _remainingSeconds--;
        try
        {
            OnTick?.Invoke();

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
        catch (Exception ex)
        {
            // Event handlers run on a timer thread — one bad subscriber must not
            // kill the countdown.
            logger.LogError(ex, "Inactivity timer handler failed");
        }
    }

    private async Task ListenForActivityAsync()
    {
        try
        {
            var script = @"
                (function() {
                    if (window.__fshInactivityWired) return;
                    window.__fshInactivityWired = true;
                    let timer;
                    const reset = () => {
                        if (timer) clearTimeout(timer);
                        timer = setTimeout(() => DotNet.invokeMethodAsync('FSH.BlazorShared', 'OnUserActivity'), 100);
                    };
                    ['click', 'keydown', 'mousemove', 'touchstart', 'scroll', 'wheel'].forEach(e => window.addEventListener(e, reset, { passive: true }));
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
    public static void OnUserActivity() => _live?.ResetFromActivity();

    private void ResetFromActivity()
    {
        // Passive activity keeps the session alive but must NOT close an open
        // warning (React parity: the tab showing the prompt stops recording
        // passive activity so the prompt stays meaningful).
        if (_warningShown)
        {
            return;
        }

        _remainingSeconds = IdleSeconds;
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        await Task.CompletedTask;
    }
}
