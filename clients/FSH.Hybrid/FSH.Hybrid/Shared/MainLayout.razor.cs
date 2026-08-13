using System.Security.Claims;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Components;
using FSH.BlazorShared.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Hybrid.Shared;

public sealed partial class MainLayout : IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
    [Inject] private FshThemeService Theme { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private string _userName = string.Empty;
    private string _userEmail = string.Empty;
    private string _tenantName = string.Empty;
    private readonly HashSet<string> _permissions = new(StringComparer.Ordinal);

    protected override void OnInitialized()
    {
        Auth.AuthenticationStateChanged += OnAuthenticationStateChanged;
        Nav.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await EvaluateUserAsync(Auth.GetAuthenticationStateAsync());
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        => await EvaluateUserAsync(task);

    private async Task EvaluateUserAsync(Task<AuthenticationState> task)
    {
        try
        {
            var state = await task;
            var claims = state.User.Claims.ToList();

            _permissions.Clear();
            foreach (var claim in claims.Where(c => c.Type == "permission"))
            {
                _permissions.Add(claim.Value);
            }

            _userName = state.User.Identity?.IsAuthenticated == true
                ? claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty
                : string.Empty;
            _userEmail = state.User.Identity?.IsAuthenticated == true
                ? claims.FirstOrDefault(c => c.Type is "email" or ClaimTypes.Email)?.Value ?? string.Empty
                : string.Empty;
            _tenantName = claims.FirstOrDefault(c => c.Type == "tenant")?.Value ?? string.Empty;

            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Auth state task faulted (e.g. malformed token) - keep the previous user state.
        }
    }

    private string UserInitials
    {
        get
        {
            var display = string.IsNullOrEmpty(_userName) ? _userEmail : _userName;
            var parts = display.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : (display.Length > 0 ? display[..Math.Min(2, display.Length)].ToUpperInvariant() : "U");
        }
    }

    private Task SetThemeLightAsync() => Theme.SetModeAsync(ThemeMode.Light);
    private Task SetThemeDarkAsync() => Theme.SetModeAsync(ThemeMode.Dark);
    private Task SetThemeSystemAsync() => Theme.SetModeAsync(ThemeMode.System);

    private async Task HandleUserMenuKey(KeyboardEventArgs e, MenuContext ctx)
    {
        if (e.Key is "Enter" or " " or "Spacebar")
        {
            await ctx.ToggleAsync(new MouseEventArgs());
        }
    }

    private void GoHome() => Nav.NavigateTo("/");

    private async Task ConfirmSignOutAsync()
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

        var authProvider = (AuthStateProvider)Auth;
        await authProvider.NotifyLogoutAsync();
        Nav.NavigateTo("/login", forceLoad: false);
    }

    public void Dispose()
    {
        Auth.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        Nav.LocationChanged -= OnLocationChanged;
    }
}
