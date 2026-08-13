using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Components;
using FSH.BlazorShared.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Shared;

/// <summary>
/// Command palette — Ctrl+K / Cmd+K (or the topbar search button) opens a
/// MudAutocomplete quick-nav over every destination in <see cref="NavSpec"/>
/// plus settings sub-pages, theme switching and sign-out. React parity:
/// clients/dashboard/src/components/command-palette/.
/// </summary>
public sealed partial class CommandPalette : IDisposable
{
    public sealed record PaletteItem(
        string Id,
        string Label,
        string? Hint,
        string? Href,
        string? Permission,
        IReadOnlyList<string>? AnyPermissions,
        string? Keywords,
        string Icon);

    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
    [Inject] private AuthStateProvider AuthState { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private FshThemeService Theme { get; set; } = default!;

    /// <summary>Only instance of the palette in the app; the JS shortcut relay targets it.</summary>
    public static CommandPalette? Current { get; private set; }

    private static bool _shortcutListenerInstalled;

    private readonly HashSet<string> _permissions = new(StringComparer.Ordinal);
    private IReadOnlyList<PaletteItem> _visibleItems = [];
    private string _query = string.Empty;
    private int _highlight;
    private bool _open;
    private ElementReference _searchInput;

    private List<PaletteItem> FilteredItems
    {
        get
        {
            var term = _query.Trim();
            return string.IsNullOrEmpty(term)
                ? _visibleItems.ToList()
                : _visibleItems.Where(item =>
                    item.Label.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (item.Hint?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Keywords?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
    }

    protected override void OnInitialized()
    {
        Current = this;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync();
        _visibleItems = BuildItems();
        await EnsureShortcutListenerAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_open && _query.Length == 0)
        {
            await _searchInput.FocusAsync();
        }
    }

    private void OnQueryChanged(ChangeEventArgs e)
    {
        _query = e.Value?.ToString() ?? string.Empty;
        _highlight = 0;
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Escape":
                Close();
                break;
            case "ArrowDown":
                _highlight = Math.Min(_highlight + 1, FilteredItems.Count - 1);
                break;
            case "ArrowUp":
                _highlight = Math.Max(_highlight - 1, 0);
                break;
            case "Enter":
                if (FilteredItems.Count > 0)
                {
                    await SelectAsync(FilteredItems[Math.Clamp(_highlight, 0, FilteredItems.Count - 1)]);
                }

                break;
        }
    }

    private async Task LoadPermissionsAsync()
    {
        try
        {
            var state = await Auth.GetAuthenticationStateAsync();
            _permissions.Clear();
            foreach (var claim in state.User.Claims.Where(c => c.Type == "permission"))
            {
                _permissions.Add(claim.Value);
            }
        }
        catch
        {
            // Auth state task faulted — keep the empty permission set (gated items hidden).
        }
    }

    private bool CanSee(string? permission, IReadOnlyList<string>? anyPermissions)
        => (permission is null || _permissions.Contains(permission))
           && (anyPermissions is null || anyPermissions.Any(_permissions.Contains));

    private IReadOnlyList<PaletteItem> BuildItems()
    {
        var items = new List<PaletteItem>();

        foreach (var item in NavSpec.TopItems)
        {
            AddNavItem(items, item, hint: null);
        }

        foreach (var section in NavSpec.Sections)
        {
            foreach (var item in section.Items)
            {
                AddNavItem(items, item, hint: section.Caption);
            }
        }

        foreach (var item in NavSpec.BottomItems)
        {
            AddNavItem(items, item, hint: null);
        }

        foreach (var account in AccountItems)
        {
            if (CanSee(account.Permission, account.AnyPermissions))
            {
                items.Add(account);
            }
        }

        items.Add(new("theme-light", "Switch to light", "Theme", null, null, null, "bright day", Icons.Material.Filled.LightMode));
        items.Add(new("theme-dark", "Switch to dark", "Theme", null, null, null, "night oled", Icons.Material.Filled.DarkMode));
        items.Add(new("theme-system", "Follow system theme", "Theme", null, null, null, "auto", Icons.Material.Filled.BrightnessAuto));
        items.Add(new("session-logout", "Sign out", "End this session", null, null, null, "logout exit quit", Icons.Material.Filled.Logout));

        return items;
    }

    private void AddNavItem(List<PaletteItem> items, NavSpec.NavItem item, string? hint)
    {
        if (!CanSee(item.Permission, item.AnyPermissions))
        {
            return;
        }

        var keywords = ExtraKeywords.TryGetValue(item.Href, out var extra)
            ? $"{item.Label} {extra}"
            : item.Label;
        items.Add(new(
            $"nav-{item.Href.Replace('/', '-').Trim('-')}",
            item.Label,
            hint,
            item.Href,
            null,
            null,
            keywords,
            item.Icon));
    }

    // Settings sub-pages, mirroring the React palette's Account group.
    private static readonly PaletteItem[] AccountItems =
    [
        new("acc-profile", "Profile", "Name, email, contact", "/settings/profile", null, null, "name email contact", Icons.Material.Filled.Person),
        new("acc-security", "Security", "Password, 2FA, sessions", "/settings/security", null, null, "password 2fa sessions", Icons.Material.Filled.Shield),
        new("acc-keys", "API keys", "Generate & rotate", "/settings/api-keys", null, null, "token credentials", Icons.Material.Filled.Key),
        new("acc-notifications", "Notifications", "Email preferences", "/settings/notifications", null, null, "email preferences", Icons.Material.Filled.Notifications),
        new("acc-appearance", "Appearance", "Theme, font, density", "/settings/appearance", null, null, "theme font density dark light", Icons.Material.Filled.Palette),
    ];

    private static readonly IReadOnlyDictionary<string, string> ExtraKeywords = new Dictionary<string, string>
    {
        ["/"] = "home dashboard",
        ["/activity"] = "events sse live stream",
        ["/chat"] = "messages dm channel conversation",
        ["/files"] = "storage uploads documents",
        ["/identity/users"] = "identity people members team",
        ["/identity/roles"] = "identity permissions rbac",
        ["/identity/groups"] = "identity teams org",
        ["/catalog/products"] = "catalog sku inventory stock",
        ["/catalog/brands"] = "catalog",
        ["/catalog/categories"] = "catalog",
        ["/tickets"] = "support issues helpdesk",
        ["/invoices"] = "billing payment",
        ["/system/health"] = "status uptime system ready redis postgres",
        ["/system/audits"] = "audit log compliance security trace",
        ["/system/trash"] = "recycle deleted restore",
        ["/system/sessions"] = "devices logins",
        ["/settings"] = "preferences config",
    };

    private async Task SelectAsync(PaletteItem item)
    {
        _query = string.Empty;
        _highlight = 0;

        if (item.Href is not null)
        {
            Close();
            Nav.NavigateTo(item.Href);
            return;
        }

        switch (item.Id)
        {
            case "theme-light":
                await Theme.SetModeAsync(ThemeMode.Light);
                break;
            case "theme-dark":
                await Theme.SetModeAsync(ThemeMode.Dark);
                break;
            case "theme-system":
                await Theme.SetModeAsync(ThemeMode.System);
                break;
            case "session-logout":
                Close();
                await SignOutAsync();
                break;
        }
    }

    private async Task SignOutAsync()
    {
        var parameters = new DialogParameters
        {
            { "Message", "Are you sure you want to sign out?" },
            { "ConfirmText", "Sign out" },
            { "CancelText", "Cancel" },
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Sign out", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await AuthState.NotifyLogoutAsync();
        Nav.NavigateTo("/login", forceLoad: false);
    }

    public async Task OpenAsync()
    {
        _open = true;
        _query = string.Empty;
        _highlight = 0;
        await InvokeAsync(StateHasChanged);
    }

    private void Close()
    {
        _open = false;
        StateHasChanged();
    }

    private async Task EnsureShortcutListenerAsync()
    {
        if (_shortcutListenerInstalled)
        {
            return;
        }

        _shortcutListenerInstalled = true;
        try
        {
            await Js.InvokeVoidAsync("eval", ShortcutScript);
        }
        catch
        {
            // Keyboard shortcut unavailable (e.g. CSP restrictions); the topbar button still opens it.
        }
    }

    public void Dispose()
    {
        if (ReferenceEquals(Current, this))
        {
            Current = null;
        }
    }

    private const string ShortcutScript = @"
        window.addEventListener('keydown', function (e) {
            if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                DotNet.invokeMethodAsync('FSH.Dashboard.Wasm', 'OnCommandPaletteShortcut');
            }
        });
    ";
}

public static class CommandPaletteShortcutRelay
{
    [JSInvokable]
    public static void OnCommandPaletteShortcut()
    {
        _ = CommandPalette.Current?.OpenAsync();
    }
}
