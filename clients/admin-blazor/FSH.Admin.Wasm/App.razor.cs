using FSH.BlazorShared.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FSH.Admin.Wasm;

public sealed partial class App : IDisposable
{
    [Inject] private ITokenStore TokenStore { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

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

    private void OnTokensChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
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