using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

/// <summary>
/// Permission-gated UI parity: the tenant console (dashboard) does not 403 on
/// unauthorized routes - pages still render, but permission-gated navigation
/// links and actions are hidden (mirrors the React dashboard app and the
/// FshPermissionGate component). The admin app, by contrast, shows a 403
/// surface (covered by the admin E2E suite).
/// </summary>
[Collection("e2e")]
public sealed class PermissionGateTests(BlazorAppServerFixture fixture) : IAsyncLifetime
{
    private static readonly object UsersSearch = new
    {
        items = new[]
        {
            new
            {
                id = "u-bob",
                userName = "bob.patel",
                email = "bob.patel@acme.example",
                firstName = "Bob",
                lastName = "Patel",
                displayName = "Bob Patel",
                phoneNumber = (string?)null,
                isActive = true,
                isDeleted = false,
                createdAtUtc = "2026-01-15T09:00:00Z",
                updatedAtUtc = (DateTime?)null,
                deletedOnUtc = (DateTimeOffset?)null,
                deletedBy = (string?)null,
                lastLoggedInAtUtc = (DateTime?)null,
            },
        },
        pageNumber = 1,
        pageSize = 20,
        totalCount = 1,
        totalPages = 1,
        hasPrevious = false,
        hasNext = false,
    };

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

    private async Task OpenUsersAsync(string[] permissions)
    {
        await E2EHelpers.SeedAuthedSessionAsync(_page, permissions: permissions);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/identity/users/search**", UsersSearch);
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/identity/roles*", E2EHelpers.Paged(Array.Empty<object>(), 0));

        await _page.GotoAsync($"{BlazorAppServerFixture.ServerUrl}/identity/users");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
    }

    [Fact]
    public async Task Page_WithoutPermission_StillRenders_ButHidesNavLinkAndActions()
    {
        await OpenUsersAsync(["Permissions.Catalog.Products.View"]);

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Users", Exact = true })).ToBeVisibleAsync();
        await _page.GetByText("bob.patel", new() { Exact = true }).WaitForAsync();

        await Expect(_page.GetByRole(AriaRole.Link, new() { Name = "Users" })).ToHaveCountAsync(0);
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Register user", Exact = true })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Page_WithPermission_ShowsNavLink_AndRegisterAction()
    {
        await OpenUsersAsync(["Permissions.Users.Update", "Permissions.Users.Create"]);

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Users", Exact = true })).ToBeVisibleAsync();

        await Expect(_page.GetByRole(AriaRole.Link, new() { Name = "Users" })).ToHaveCountAsync(1);
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Register user", Exact = true })).ToBeVisibleAsync();
    }
}
