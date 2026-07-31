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

    protected override async Task OnInitializedAsync()
    {
        TokenStore.TokensChanged += OnTokensChanged;

        try
        {
            await Sse.StartAsync();
            _sseSub = Sse.Messages.Subscribe(new SseObserver(OnSseEvent));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SSE connection failed at startup; will be retried later");
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
        InvokeAsync(StateHasChanged);
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