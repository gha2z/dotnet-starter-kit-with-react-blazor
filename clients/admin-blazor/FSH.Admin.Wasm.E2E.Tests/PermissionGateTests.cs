using FSH.Admin.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.E2E.Tests;

/// <summary>
/// Permission gate: an authenticated operator who lacks Tenants.View must hit
/// the 403 surface (RedirectToLogin renders "403 — Access denied" instead of a
/// redirect when the user is authenticated) and the Tenants nav entry must be
/// hidden. A user WITH the permission renders the page — the positive control.
/// </summary>
[Collection("e2e")]
public sealed class PermissionGateTests(BlazorAppServerFixture fixture) : IAsyncDisposable
{
    private readonly IBrowser _browser = fixture.Browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IAsyncDisposable? _seed;

    private async Task<IPage> NewPageAsync(string[] permissions)
    {
        _context = await _browser.NewContextAsync();
        _seed = await E2EHelpers.SeedAuthedSessionAsync(_context, permissions: permissions);
        _page = await _context.NewPageAsync();
        await E2EHelpers.InstallShellMocksAsync(_page, permissions);
        return _page;
    }

    [Fact]
    public async Task UserWithoutTenantPermission_Sees403Surface_AndNoNavEntry()
    {
        var page = await NewPageAsync(["Permissions.Users.View"]);

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/tenants");
        await E2EHelpers.WaitForBlazorReadyAsync(page);
        await _seed!.DisposeAsync();

        // Authenticated but lacking the permission: 403 surface, not a redirect.
        await Expect(page.GetByText("403 — Access denied")).ToBeVisibleAsync();
        page.Url.ShouldContain("/tenants");

        // The Tenants nav item is permission-filtered out of the sidebar.
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Tenants" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task UserWithTenantPermission_RendersThePage()
    {
        var page = await NewPageAsync(E2EHelpers.AdminPerms);
        await E2EHelpers.MockJsonAsync(page, "**/api/v1/tenants/*", E2EHelpers.Paged(Array.Empty<object>(), 0));

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/tenants");
        await E2EHelpers.WaitForBlazorReadyAsync(page);
        await _seed!.DisposeAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Registry" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Tenants" })).ToBeVisibleAsync();
        await Expect(page.GetByText("403 — Access denied")).ToHaveCountAsync(0);
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
