using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FSH.Dashboard.Wasm.Pages.Settings;

public sealed partial class SettingsNotificationsPage
{
    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    private bool _busy;

    private async Task OpenBellAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        await Js.InvokeVoidAsync("fshBell.open").ConfigureAwait(false);
        _busy = false;
    }
}