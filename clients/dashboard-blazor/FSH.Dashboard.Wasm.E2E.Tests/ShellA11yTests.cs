using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

/// <summary>
/// Shell accessibility + boot experience: the branded splash must be visible
/// while the WASM runtime boots, the skip-to-content link must be the first
/// Tab stop and jump focus into the main region, and the shell must expose
/// proper landmarks and accessible names on its icon-only buttons.
/// </summary>
[Collection("e2e")]
public sealed class ShellA11yTests(BlazorAppServerFixture fixture) : IAsyncLifetime
{
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public async Task InitializeAsync()
    {
        _context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
        });
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.CloseAsync();
    }

    private async Task SeedAndOpenAppAsync(string path)
    {
        var seed = await E2EHelpers.SeedAuthedSessionAsync(_page);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await E2EHelpers.MockOverviewAsync(_page);
        await _page.GotoAsync($"{BlazorAppServerFixture.ServerUrl}{path}");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
        await seed.DisposeAsync();
    }

    [Fact]
    public async Task Splash_ShowsBrandedLoader_WhileWasmBoots()
    {
        // WaitUntilState.Commit returns as soon as the HTML document is
        // committed - before blazor.webassembly.js has executed - so the
        // static splash inside #app must be on screen.
        await _page.GotoAsync(
            $"{BlazorAppServerFixture.ServerUrl}/login",
            new PageGotoOptions { WaitUntil = WaitUntilState.Commit });

        await Expect(_page.Locator(".fsh-loading-brand")).ToBeVisibleAsync();
        await _page.GetByText("Loading your dashboard…").WaitForAsync();

        await E2EHelpers.WaitForBlazorReadyAsync(_page);

        await Expect(_page.Locator(".fsh-loading-container")).ToHaveCountAsync(0);
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SkipLink_IsFirstTabStop_AndJumpsFocusToMainContent()
    {
        await SeedAndOpenAppAsync("/");
        await Expect(_page.Locator(".fsh-sidebar")).ToBeVisibleAsync();

        // The skip link is the first focusable element in the authenticated
        // shell (the first Tab stop after the page loads).
        await _page.Keyboard.PressAsync("Tab");
        await Expect(_page.Locator(".fsh-skip-link")).ToBeFocusedAsync();

        // The link must point at the main content region, which is a focusable
        // (tabindex="-1") target for programmatic focus.
        (await _page.Locator(".fsh-skip-link").GetAttributeAsync("href")).ShouldBe("#fsh-main");
        (await _page.Locator("#fsh-main").GetAttributeAsync("tabindex")).ShouldBe("-1");

        // Enter must move focus into the main region (MainLayout prevents the
        // Blazor SPA interceptor from swallowing the fragment navigation and
        // performs the focus via JS instead).
        await _page.Keyboard.PressAsync("Enter");
        await Expect(_page.Locator("#fsh-main")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Shell_ExposesLandmarks_AndNamedIconButtons()
    {
        await SeedAndOpenAppAsync("/");

        await Expect(_page.GetByRole(AriaRole.Main)).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Navigation, new() { Name = "Primary" })).ToBeVisibleAsync();

        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" })).ToBeVisibleAsync();
        // The drawer trigger is display:none above 900px (by design) - so it
        // has no accessible name at this viewport. Assert the attribute; the
        // role+visibility behavior is covered by the mobile viewport suite.
        (await _page.Locator(".fsh-topbar-menu").GetAttributeAsync("aria-label")).ShouldBe("Open navigation");
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Search (Ctrl+K)" })).ToBeVisibleAsync();
        // The theme button sits inside a mud-menu-activator div that mirrors
        // its accessible name - .First targets the icon button itself.
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Theme" }).First).ToBeVisibleAsync();
    }
}
