using Microsoft.JSInterop;

namespace FSH.BlazorShared.Infrastructure;

/// <summary>
/// Browser connectivity state (React parity: the dashboard app shows an offline
/// banner while <c>navigator.onLine</c> is false). Subscribes to the browser's
/// online/offline events so the UI can react to reconnects without polling.
/// </summary>
public sealed class FshNetworkStatus(IJSRuntime js) : INetworkStatus, IAsyncDisposable
{
    private readonly IJSRuntime _js = js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<FshNetworkStatus>? _ref;
    private bool _initialized;
    private bool _isOnline = true;

    /// <summary>Current connectivity state. Defaults to online until the module reports otherwise.</summary>
    public bool IsOnline => _isOnline;

    /// <summary>Raised whenever the browser reports an online/offline transition.</summary>
    public event Action? StatusChanged;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshNetwork.js");
            _isOnline = await _module.InvokeAsync<bool>("isOnline");
            _ref ??= DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("watch", _ref);
        }
        catch
        {
            // JS unavailable (e.g. tests/prerender) - stay "online".
        }
    }

    [JSInvokable]
    public void OnStatusChanged(bool isOnline)
    {
        if (_isOnline == isOnline)
        {
            return;
        }

        _isOnline = isOnline;
        StatusChanged?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            _ref?.Dispose();
            return _module?.DisposeAsync() ?? ValueTask.CompletedTask;
        }
        catch
        {
            return ValueTask.CompletedTask;
        }
    }
}
