using System.Text.Json;
using FSH.Admin.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.E2E.Tests;

/// <summary>
/// Critical auth paths: login (form → dashboard), login failure, logout
/// (tokens cleared → /login), and cross-tab logout (storage event → second
/// tab reloads and lands on /login). All API traffic is route-mocked; no
/// backend is involved.
/// </summary>
[Collection("e2e")]
public sealed class AuthFlowTests(BlazorAppServerFixture fixture) : IAsyncDisposable
{
    private readonly IBrowser _browser = fixture.Browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IAsyncDisposable? _seed;

    private async Task<IPage> NewPageAsync(bool authed = false)
    {
        _context = await _browser.NewContextAsync();
        if (authed)
        {
            _seed = await E2EHelpers.SeedAuthedSessionAsync(_context);
        }

        _page = await _context.NewPageAsync();
        await E2EHelpers.InstallShellMocksAsync(_page);
        return _page;
    }

    /// <summary>Disposes the seed registration after the first page load so full reloads don't re-authenticate.</summary>
    private async Task DisposeSeedAsync()
    {
        if (_seed is not null)
        {
            await _seed.DisposeAsync();
            _seed = null;
        }
    }

    private async Task SignOutAsync(IPage page)
    {
        // Open the user menu (initials tile) and click "Sign out".
        await page.Locator(".fsh-user-menu-tile").ClickAsync();
        await page.Locator(".mud-menu-item", new PageLocatorOptions { HasText = "Sign out" }).ClickAsync();

        // Confirmation dialog (FshConfirmDialogContent) — confirm with its filled button.
        await page.Locator(".mud-dialog").WaitForAsync();
        await page.Locator(".mud-dialog")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Sign out" })
            .ClickAsync();
    }

    [Fact]
    public async Task Login_FillsForm_Submits_AndLandsOnDashboard()
    {
        var page = await NewPageAsync();
        await E2EHelpers.MockJsonAsync(page, "**/api/v1/identity/token/issue", E2EHelpers.LoginTokenResponse());
        await E2EHelpers.MockOverviewAsync(page);

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/login");
        await E2EHelpers.WaitForBlazorReadyAsync(page);

        // Brand lockup + form render.
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();
        await Expect(page.GetByText("Platform Admin")).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Tenant")).ToHaveValueAsync("root");
        await Expect(page.GetByLabel("Email")).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Password", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })).ToBeVisibleAsync();

        // Fill + submit; capture the token/issue request.
        var requestTask = page.WaitForRequestAsync(
            r => r.Url.Contains("/api/v1/identity/token/issue") && r.Method == "POST",
            new PageWaitForRequestOptions { Timeout = 15_000 });
        await E2EHelpers.FillAndBlurAsync(page.GetByLabel("Email"), "operator@root.example");
        await E2EHelpers.FillAndBlurAsync(page.GetByLabel("Password", new() { Exact = true }), "Sup3rSecret!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        var request = await requestTask;
        request.Headers["x-fsh-app"].ShouldBe("admin");
        request.Headers["tenant"].ShouldBe("root");
        var body = JsonDocument.Parse(request.PostData ?? "{}").RootElement;
        body.GetProperty("email").GetString().ShouldBe("operator@root.example");
        body.GetProperty("password").GetString().ShouldBe("Sup3rSecret!");

        // Landed on the dashboard (overview) with a hydrated nav.
        await page.WaitForURLAsync(BlazorAppServerFixture.ServerUrl + "/", new() { Timeout = 30_000 });
        await Expect(page.Locator(".fsh-sidebar")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Tenants" })).ToBeVisibleAsync();
        (await E2EHelpers.GetLocalStorageAsync(page, E2EHelpers.AccessKey)).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsError_AndStaysOnLogin()
    {
        var page = await NewPageAsync();
        await E2EHelpers.MockJsonAsync(
            page,
            "**/api/v1/identity/token/issue",
            new { title = "Unauthorized", status = 401, detail = "Invalid credentials." },
            status: 401);

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/login");
        await E2EHelpers.WaitForBlazorReadyAsync(page);

        await E2EHelpers.FillAndBlurAsync(page.GetByLabel("Email"), "operator@root.example");
        await E2EHelpers.FillAndBlurAsync(page.GetByLabel("Password", new() { Exact = true }), "wrong-password");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(page.GetByText("Login failed (401)")).ToBeVisibleAsync();
        page.Url.ShouldContain("/login");
    }

    [Fact]
    public async Task Logout_ClearsTokens_AndRedirectsToLogin()
    {
        var page = await NewPageAsync(authed: true);
        await E2EHelpers.MockOverviewAsync(page);

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/");
        await E2EHelpers.WaitForBlazorReadyAsync(page);
        await DisposeSeedAsync();
        await Expect(page.Locator(".fsh-sidebar")).ToBeVisibleAsync();

        await SignOutAsync(page);

        await E2EHelpers.WaitForUrlPrefixAsync(page, BlazorAppServerFixture.ServerUrl + "/login");
        await E2EHelpers.WaitForBlazorReadyAsync(page);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();

        (await E2EHelpers.GetLocalStorageAsync(page, E2EHelpers.AccessKey)).ShouldBeNull();
        (await E2EHelpers.GetLocalStorageAsync(page, E2EHelpers.RefreshKey)).ShouldBeNull();
        (await E2EHelpers.GetLocalStorageAsync(page, E2EHelpers.PermissionsKey)).ShouldBeNull();
    }

    [Fact]
    public async Task CrossTabLogout_SecondTab_RedirectsToLogin()
    {
        var context = await _browser.NewContextAsync();
        await using var _ = context;
        var seed = await E2EHelpers.SeedAuthedSessionAsync(context);

        var firstTab = await context.NewPageAsync();
        await E2EHelpers.InstallShellMocksAsync(firstTab);
        await E2EHelpers.MockOverviewAsync(firstTab);
        await firstTab.GotoAsync(BlazorAppServerFixture.ServerUrl + "/");
        await E2EHelpers.WaitForBlazorReadyAsync(firstTab);
        await Expect(firstTab.Locator(".fsh-sidebar")).ToBeVisibleAsync();

        var secondTab = await context.NewPageAsync();
        await E2EHelpers.InstallShellMocksAsync(secondTab);
        await E2EHelpers.MockOverviewAsync(secondTab);
        await secondTab.GotoAsync(BlazorAppServerFixture.ServerUrl + "/");
        await E2EHelpers.WaitForBlazorReadyAsync(secondTab);
        await seed.DisposeAsync();
        await Expect(secondTab.Locator(".fsh-sidebar")).ToBeVisibleAsync();

        // Sign out in the first tab — the storage event must push the second tab
        // through a reload (token gone) and back to /login.
        await SignOutAsync(firstTab);
        await E2EHelpers.WaitForUrlPrefixAsync(firstTab, BlazorAppServerFixture.ServerUrl + "/login", timeoutMs: 15_000);

        await E2EHelpers.WaitForUrlPrefixAsync(secondTab, BlazorAppServerFixture.ServerUrl + "/login", timeoutMs: 60_000);
    }

    public async ValueTask DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }

        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }
}
