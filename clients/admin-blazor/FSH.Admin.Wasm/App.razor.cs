using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FSH.Admin.Wasm;

public sealed partial class App : IDisposable
{
    [Inject] private ITokenStore TokenStore { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private FshThemeService Theme { get; set; } = default!;

    protected override void OnInitialized()
    {
        Theme.Changed += OnThemeChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        TokenStore.TokensChanged += OnTokensChanged;

        try
        {
            await Js.InvokeVoidAsync("eval", CrossTabLogoutScript);
        }
        catch
        {
            // Cross-tab logout not available (e.g. CSP restrictions)
        }
    }

    private void OnThemeChanged()
    {
        _theme = FshMudTheme.CreateAdmin(FshAppearanceOptions.GetFont(Theme.FontId));
        InvokeAsync(StateHasChanged);
    }

    private void OnTokensChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Theme.Changed -= OnThemeChanged;
        TokenStore.TokensChanged -= OnTokensChanged;
    }

    private const string CrossTabLogoutScript = @"
        window.addEventListener('storage', function(e) {
            if (e.key === 'fsh.admin.accessToken' && e.newValue === null) {
                window.location.reload();
            }
        });
    ";
}