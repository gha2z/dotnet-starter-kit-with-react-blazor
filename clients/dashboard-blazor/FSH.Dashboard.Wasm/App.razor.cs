using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Sse;
using FSH.Dashboard.Wasm.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FSH.Dashboard.Wasm;

public sealed partial class App : IDisposable
{
    [Inject] private ITokenStore TokenStore { get; set; } = default!;
    [Inject] private ISseService Sse { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private ImpersonationHandoff ImpersonationHandoff { get; set; } = default!;
    [Inject] private LazyAssemblyLoader LazyLoader { get; set; } = default!;
    [Inject] private ILogger<App> Logger { get; set; } = default!;

    private IDisposable? _sseSub;
    private bool _sseStarting;
    private bool _pageAssemblyLoaded;
    private bool _loadingPages;

    private async Task OnNavigateAsync(NavigationContext context)
    {
        if (_pageAssemblyLoaded || context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        _loadingPages = true;
        StateHasChanged();
        try
        {
            _lazyAssemblies.AddRange(await LazyLoader.LoadAssembliesAsync(PagesAssemblyPaths));
            _pageAssemblyLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to lazy-load the pages assembly for {Route}", context.Path);
        }
        finally
        {
            _loadingPages = false;
            StateHasChanged();
        }
    }

    private static readonly string[] PagesAssemblyPaths = ["FSH.Dashboard.Pages.wasm"];

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
            await Js.InvokeVoidAsync("eval", CrossTabLogoutScript);
        }
        catch
        {
            // Cross-tab logout not available (e.g. CSP restrictions)
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
        if (string.IsNullOrEmpty(hash) || !hash.StartsWith("#impersonate", StringComparison.Ordinal))
        {
            return;
        }

        // Runs during App bootstrap (before the Router's first render) so the
        // impersonation session exists before any protected route asks for auth state.
        await ImpersonationHandoff.InstallFromHashAsync(hash);

        // Scrub the fragment (React parity: stripHash via history.replaceState) so the
        // token can't linger in the URL bar or browser history.
        await Js.InvokeVoidAsync("eval", ImpersonationHashStripScript);
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

    private const string ImpersonationHashStripScript = @"
        history.replaceState(null, '', location.pathname + location.search);
    ";

    private const string CrossTabLogoutScript = @"
        window.addEventListener('storage', function(e) {
            if (e.key === 'fsh.dashboard.accessToken' || e.key === 'fsh.dashboard.impersonation.accessToken') {
                if (e.newValue === null) {
                    window.location.reload();
                }
            }
        });
    ";
}