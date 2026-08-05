using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

/// <summary>
/// Mobile viewport (375x812) smoke: the shell and the key list pages must not
/// overflow horizontally, the drawer trigger must be available, and the
/// responsive grid media queries must keep rows on one screen width.
/// </summary>
[Collection("e2e")]
public sealed class MobileViewportTests(BlazorAppServerFixture fixture) : IAsyncLifetime
{
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public async Task InitializeAsync()
    {
        _context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
            ViewportSize = new() { Width = 375, Height = 812 },
        });
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.CloseAsync();
    }

    private async Task AssertNoHorizontalOverflowAsync()
    {
        var overflow = await _page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        overflow.ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task Overview_Mobile_NoHorizontalOverflow_AndDrawerTriggerVisible()
    {
        var seed = await E2EHelpers.SeedAuthedSessionAsync(_page);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await E2EHelpers.MockOverviewAsync(_page);
        await _page.GotoAsync(BlazorAppServerFixture.ServerUrl);
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Overview" })).ToBeVisibleAsync();
        await seed.DisposeAsync();

        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Open navigation" })).ToBeVisibleAsync();
        await AssertNoHorizontalOverflowAsync();
    }

    [Fact]
    public async Task Users_Mobile_NoHorizontalOverflow()
    {
        var seed = await E2EHelpers.SeedAuthedSessionAsync(_page);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await _page.RouteAsync("**/api/v1/identity/users/search**", route =>
            E2EHelpers.RouteJsonAsync(route, E2EHelpers.Paged(Array.Empty<object>(), 0)));
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/identity/roles*", E2EHelpers.Paged(Array.Empty<object>(), 0));

        await _page.GotoAsync($"{BlazorAppServerFixture.ServerUrl}/identity/users");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
        await seed.DisposeAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Users", Exact = true })).ToBeVisibleAsync();
        await AssertNoHorizontalOverflowAsync();
    }

    [Fact]
    public async Task Products_Mobile_NoHorizontalOverflow()
    {
        var seed = await E2EHelpers.SeedAuthedSessionAsync(_page);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/catalog/products*", E2EHelpers.Paged(Array.Empty<object>(), 0));
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/catalog/brands*", E2EHelpers.Paged(Array.Empty<object>(), 0));
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/catalog/categories*", E2EHelpers.Paged(Array.Empty<object>(), 0));

        await _page.GotoAsync($"{BlazorAppServerFixture.ServerUrl}/catalog/products");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
        await seed.DisposeAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Products", Exact = true })).ToBeVisibleAsync();
        await AssertNoHorizontalOverflowAsync();
    }
}
