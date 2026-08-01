using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Sse;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FSH.Dashboard.Wasm;

public sealed partial class App : IDisposable
{
    [Inject] private ITokenStore TokenStore { get; set; } = default!;
    [Inject] private ISseService Sse { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private ILogger<App> Logger { get; set; } = default!;

    private IDisposable? _sseSub;
    private bool _sseStarting;

    protected override async Task OnInitializedAsync()
    {
        TokenStore.TokensChanged += OnTokensChanged;
        _sseSub = Sse.Messages.Subscribe(new SseObserver(OnSseEvent));

        try
        {
            await EnsureSseConnectionAsync();
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "SSE connection failed at startup; will retry after login");
        }

        try
        {
            await CheckImpersonationHashAsync();
        }
        catch
        {
            // Impersonation check not critical
        }
    }

    private void OnSseEvent(SseEvent msg)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task CheckImpersonationHashAsync()
    {
        var hash = await Js.InvokeAsync<string>("eval", "window.location.hash");
        if (hash?.StartsWith("#impersonation:") == true)
        {
            var token = hash["#impersonation:".Length..];
            await Js.InvokeVoidAsync("eval", "window.location.hash = ''");
        }
    }

    private void OnTokensChanged()
    {
        InvokeAsync(async () =>
        {
            StateHasChanged();

            if (await TokenStore.GetAccessTokenAsync() is null)
            {
                await Sse.StopAsync();
                return;
            }

            try
            {
                await EnsureSseConnectionAsync();
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "SSE connection failed after login; will retry on next token change");
            }
        });
    }

    private async Task EnsureSseConnectionAsync()
    {
        if (_sseStarting || Sse.IsConnected)
        {
            return;
        }

        if (await TokenStore.GetAccessTokenAsync() is null)
        {
            return;
        }

        _sseStarting = true;
        try
        {
            await Sse.StartAsync();
        }
        finally
        {
            _sseStarting = false;
        }
    }

    public void Dispose()
    {
        TokenStore.TokensChanged -= OnTokensChanged;
        _sseSub?.Dispose();
    }

    private sealed class SseObserver(Action<SseEvent> onNext) : IObserver<SseEvent>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(SseEvent value) => onNext(value);
    }
}