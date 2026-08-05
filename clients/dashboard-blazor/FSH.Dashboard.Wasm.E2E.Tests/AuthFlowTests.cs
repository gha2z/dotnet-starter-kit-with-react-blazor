using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

/// <summary>
/// Critical auth paths: login (form + dashboard flow), login failure, logout
/// (tokens cleared + /login), and cross-tab logout (storage event pushes a
/// second tab through a reload back to /login). All API traffic is
/// route-mocked; no backend is involved.
/// </summary>
[Collection("e2e")]
public sealed class AuthFlowTests(BlazorAppServerFixture fixture) : IAsyncLifetime
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

    private async Task OpenLoginAsync()
    {
        await _page.GotoAsync($"{BlazorAppServerFixture.ServerUrl}/login");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();
    }

    private async Task FillCredentialsAsync(string tenant, string email, string password)
    {
        await E2EHelpers.FillAndBlurAsync(_page.GetByLabel("Tenant"), tenant);
        await E2EHelpers.FillAndBlurAsync(_page.GetByLabel("Email"), email);
        await E2EHelpers.FillAndBlurAsync(_page.GetByLabel("Password", new() { Exact = true }), password);
    }

    [Fact]
    public async Task Login_WithValidCredentials_SignsIn_AndNavigatesToOverview()
    {
        await E2EHelpers.MockJsonAsync(
            _page,
            "**/api/v1/identity/token/issue",
            E2EHelpers.LoginTokenResponse());
        await E2EHelpers.MockOverviewAsync(_page);

        await OpenLoginAsync();

        var requestTask = _page.WaitForRequestAsync(
            r => r.Url.Contains("/api/v1/identity/token/issue") && r.Method == "POST",
            new PageWaitForRequestOptions { Timeout = 15_000 });

        await FillCredentialsAsync("acme", "operator@acme.example", "Sup3rSecret!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        var request = await requestTask;
        request.Headers["x-fsh-app"].ShouldBe("dashboard");
        request.Headers["tenant"].ShouldBe("acme");

        await E2EHelpers.WaitForUrlPrefixAsync(_page, $"{BlazorAppServerFixture.ServerUrl}/");
        await Expect(_page.Locator(".fsh-sidebar")).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Overview" })).ToBeVisibleAsync();

        var access = await E2EHelpers.GetLocalStorageAsync(_page, E2EHelpers.AccessKey);
        access.ShouldNotBeNullOrEmpty();
        var tenant = await E2EHelpers.GetLocalStorageAsync(_page, E2EHelpers.TenantKey);
        tenant.ShouldBe("acme");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsError_AndStaysOnLogin()
    {
        await E2EHelpers.MockJsonAsync(
            _page,
            "**/api/v1/identity/token/issue",
            new { title = "Unauthorized", status = 401, detail = "Invalid credentials." },
            status: 401);

        await OpenLoginAsync();

        await FillCredentialsAsync("acme", "operator@acme.example", "wrong-password");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(_page.GetByText("Login failed (401)")).ToBeVisibleAsync();
        _page.Url.ShouldContain("/login");
    }

    [Fact]
    public async Task SignOut_FromUserMenu_Confirms_AndReturnsToLogin()
    {
        var seed = await E2EHelpers.SeedAuthedSessionAsync(_page);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await E2EHelpers.MockOverviewAsync(_page);

        await _page.GotoAsync(BlazorAppServerFixture.ServerUrl);
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
        await Expect(_page.Locator(".fsh-sidebar")).ToBeVisibleAsync();
        await seed.DisposeAsync();

        // Open the user menu (initials tile) and click "Sign out".
        await _page.Locator(".fsh-user-menu-tile").ClickAsync();
        await _page.Locator(".mud-menu-item", new PageLocatorOptions { HasText = "Sign out" }).ClickAsync();

        // Confirmation dialog (FshConfirmDialogContent) - confirm with its filled button.
        await _page.Locator(".mud-dialog").WaitForAsync();
        await _page.Locator(".mud-dialog")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Sign out" })
            .ClickAsync();

        await E2EHelpers.WaitForUrlPrefixAsync(_page, $"{BlazorAppServerFixture.ServerUrl}/login");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();

        (await E2EHelpers.GetLocalStorageAsync(_page, E2EHelpers.AccessKey)).ShouldBeNull();
        (await E2EHelpers.GetLocalStorageAsync(_page, E2EHelpers.RefreshKey)).ShouldBeNull();
    }

    [Fact]
    public async Task CrossTabLogout_SecondTab_RedirectsToLogin()
    {
        var context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
        });
        await using var _ = context;

        var firstTab = await context.NewPageAsync();
        var firstTabSeed = await E2EHelpers.SeedAuthedSessionAsync(firstTab);
        await E2EHelpers.InstallShellMocksAsync(firstTab);
        await E2EHelpers.MockOverviewAsync(firstTab);
        await firstTab.GotoAsync(BlazorAppServerFixture.ServerUrl);
        await E2EHelpers.WaitForBlazorReadyAsync(firstTab);
        await Expect(firstTab.Locator(".fsh-sidebar")).ToBeVisibleAsync();

        var secondTab = await context.NewPageAsync();
        var secondTabSeed = await E2EHelpers.SeedAuthedSessionAsync(secondTab);
        await E2EHelpers.InstallShellMocksAsync(secondTab);
        await E2EHelpers.MockOverviewAsync(secondTab);
        await secondTab.GotoAsync(BlazorAppServerFixture.ServerUrl);
        await E2EHelpers.WaitForBlazorReadyAsync(secondTab);
        await Expect(secondTab.Locator(".fsh-sidebar")).ToBeVisibleAsync();

        // Seed registrations re-run on every full reload (cross-tab logout
        // force-loads both tabs), so dispose them now that both tabs are up.
        await firstTabSeed.DisposeAsync();
        await secondTabSeed.DisposeAsync();

        // Sign out in the first tab - the storage event must push the second
        // tab through a reload (token gone) and back to /login.
        await firstTab.Locator(".fsh-user-menu-tile").ClickAsync();
        await firstTab.Locator(".mud-menu-item", new PageLocatorOptions { HasText = "Sign out" }).ClickAsync();
        await firstTab.Locator(".mud-dialog").WaitForAsync();
        await firstTab.Locator(".mud-dialog")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Sign out" })
            .ClickAsync();

        await E2EHelpers.WaitForUrlPrefixAsync(firstTab, $"{BlazorAppServerFixture.ServerUrl}/login", timeoutMs: 15_000);
        await E2EHelpers.WaitForUrlPrefixAsync(secondTab, $"{BlazorAppServerFixture.ServerUrl}/login", timeoutMs: 60_000);
    }
}
