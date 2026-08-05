using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using FSH.Admin.Wasm.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Components;

public sealed class CommandPaletteTests : TestSetup
{
    private sealed class FixedAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        private readonly Task<AuthenticationState> _state = Task.FromResult(new AuthenticationState(user));

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => _state;
    }

    private static ClaimsPrincipal Principal(params string[] permissions)
    {
        var claims = permissions.Select(p => new Claim("permission", p)).ToList();
        claims.Add(new Claim("sub", "u1"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private void Authorize(params string[] permissions)
        => Services.AddSingleton<AuthenticationStateProvider>(new FixedAuthStateProvider(Principal(permissions)));

    private void RegisterTheme()
        => Services.AddSingleton(new FSH.BlazorShared.Theming.FshThemeService(JSInterop.JSRuntime, "fsh.theme.test"));

    // The sign-out confirm uses MudBlazor's DialogService, so the dialog provider is included.
    private IRenderedComponent<Bunit.Rendering.ContainerFragment> RenderPalette(out IRenderedComponent<CommandPalette> palette)
    {
        var tree = Render(b =>
        {
            b.OpenComponent<MudDialogProvider>(0);
            b.CloseComponent();
            b.OpenComponent<CommandPalette>(1);
            b.CloseComponent();
        });
        palette = tree.FindComponent<CommandPalette>();
        return tree;
    }

    [Fact]
    public async Task Open_renders_the_search_overlay()
    {
        Authorize();
        RegisterTheme();
        var tree = RenderPalette(out var palette);

        await palette.Instance.OpenAsync();

        tree.WaitForAssertion(() => tree.Find(".fsh-command-input").ShouldNotBeNull());
        tree.Markup.ShouldContain("Type a command or search…");
    }

    [Fact]
    public async Task Search_filters_items_and_selection_navigates()
    {
        Authorize(FSH.BlazorShared.Permissions.IdentityPermissions.Users.View);
        RegisterTheme();
        var tree = RenderPalette(out var palette);
        await palette.Instance.OpenAsync();
        tree.WaitForAssertion(() => tree.Find(".fsh-command-input"));

        tree.Find(".fsh-command-input").Input("Users");

        tree.WaitForAssertion(() =>
        {
            tree.FindAll(".fsh-command-item").Count.ShouldBe(1);
            tree.Markup.ShouldContain("Users");
            tree.Markup.ShouldNotContain("Roles");
        });

        tree.Find(".fsh-command-item").Click();

        tree.WaitForAssertion(() =>
            Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith("/users"));
    }

    [Fact]
    public async Task Permission_gated_destinations_are_hidden()
    {
        Authorize("Permissions.Multitenancy.Tenants.View");
        RegisterTheme();
        var tree = RenderPalette(out var palette);
        await palette.Instance.OpenAsync();
        tree.WaitForAssertion(() => tree.Find(".fsh-command-input"));

        tree.Find(".fsh-command-input").Input("Tenants");

        tree.WaitForAssertion(() => tree.Markup.ShouldContain("No matches"));
        tree.Markup.ShouldNotContain(">Tenants<");
    }

    [Fact]
    public async Task Theme_actions_apply_and_keep_the_palette_open()
    {
        Authorize();
        RegisterTheme();
        var tree = RenderPalette(out var palette);
        await palette.Instance.OpenAsync();
        tree.WaitForAssertion(() => tree.Find(".fsh-command-input"));

        tree.Find(".fsh-command-input").Input("switch to dark");
        tree.WaitForAssertion(() => tree.FindAll(".fsh-command-item").Count.ShouldBe(1));
        tree.Find(".fsh-command-item").Click();

        var theme = Services.GetRequiredService<FSH.BlazorShared.Theming.FshThemeService>();
        theme.IsDarkMode.ShouldBeTrue();
        tree.WaitForAssertion(() => tree.Find(".fsh-command-input").ShouldNotBeNull());
    }

    [Fact]
    public async Task Sign_out_asks_for_confirmation_then_logs_out()
    {
        Authorize();
        RegisterTheme();
        var tree = RenderPalette(out var palette);
        await palette.Instance.OpenAsync();
        tree.WaitForAssertion(() => tree.Find(".fsh-command-input"));

        tree.Find(".fsh-command-input").Input("sign out");
        tree.WaitForAssertion(() => tree.FindAll(".fsh-command-item").Count.ShouldBe(1));
        tree.Find(".fsh-command-item").Click();

        tree.WaitForAssertion(() => tree.Markup.ShouldContain("Are you sure you want to sign out?"));

        var buttons = tree.FindAll(".mud-dialog button");
        buttons[buttons.Count - 1].Click();

        tree.WaitForAssertion(() =>
            Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith("/login"));
    }

    [Fact]
    public async Task Arrow_keys_move_highlight_and_enter_selects()
    {
        Authorize(
            FSH.BlazorShared.Permissions.IdentityPermissions.Users.View,
            FSH.BlazorShared.Permissions.IdentityPermissions.Roles.View,
            FSH.BlazorShared.Permissions.IdentityPermissions.Impersonation.View);
        RegisterTheme();
        var tree = RenderPalette(out var palette);
        await palette.Instance.OpenAsync();
        tree.WaitForAssertion(() => tree.Find(".fsh-command-input"));

        var input = tree.Find(".fsh-command-input");
        input.Input("identity");
        tree.WaitForAssertion(() => tree.FindAll(".fsh-command-item").Count.ShouldBe(3));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        tree.WaitForAssertion(() =>
            Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith("/roles"));
    }

    [Fact]
    public void Shortcut_relay_opens_the_palette()
    {
        Authorize();
        RegisterTheme();
        var tree = RenderPalette(out _);

        CommandPaletteShortcutRelay.OnCommandPaletteShortcut();

        tree.WaitForAssertion(() => tree.Find(".fsh-command-input").ShouldNotBeNull());
    }

    [Fact]
    public void Dispose_clears_the_static_current_reference()
    {
        Authorize();
        RegisterTheme();
        var tree = RenderPalette(out var palette);

        palette.Instance.Dispose();

        CommandPalette.Current.ShouldBeNull();
    }
}
