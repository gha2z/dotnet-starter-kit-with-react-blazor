using FSH.BlazorShared.Auth;
using Microsoft.AspNetCore.Components;

namespace FSH.Dashboard.Wasm.Pages.Terminal;

public sealed partial class ImpersonationEndedPage
{
    [Inject] private AuthStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private async Task BackToSignInAsync()
    {
        // Drop the now-useless impersonation token so /login starts clean and a stale
        // token can't bounce the user straight back into a 401 loop.
        await AuthState.NotifyLogoutAsync();
        Nav.NavigateTo("/login");
    }
}
