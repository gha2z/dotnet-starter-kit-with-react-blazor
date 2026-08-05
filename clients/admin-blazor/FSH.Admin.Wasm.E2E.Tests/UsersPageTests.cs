using System.Text.Json;
using FSH.Admin.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.E2E.Tests;

/// <summary>
/// Users directory: table renders, search issues filtered requests, and the
/// create dialog POSTs to /api/v1/identity/register then lands on the new
/// user's detail page. Fully route-mocked.
/// </summary>
[Collection("e2e")]
public sealed class UsersPageTests(BlazorAppServerFixture fixture) : IAsyncDisposable
{
    private readonly IBrowser _browser = fixture.Browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IAsyncDisposable? _seed;

    private static readonly object[] Users =
    [
        new
        {
            id = "u-bob",
            userName = "bob.patel",
            firstName = "Bob",
            lastName = "Patel",
            email = "bob.patel@root.com",
            isActive = true,
            emailConfirmed = true,
            phoneNumber = "+1 555 0100",
            imageUrl = (string?)null,
            twoFactorEnabled = false,
        },
        new
        {
            id = "u-dana",
            userName = "dana.lee",
            firstName = "Dana",
            lastName = "Lee",
            email = "dana.lee@root.com",
            isActive = false,
            emailConfirmed = false,
            phoneNumber = (string?)null,
            imageUrl = (string?)null,
            twoFactorEnabled = false,
        },
    ];

    private static readonly object[] Roles =
    [
        new { id = "role-admin", name = "Admin", description = "Full access", permissions = (string[]?)null },
    ];

    private async Task<IPage> NewUsersPageAsync()
    {
        _context = await _browser.NewContextAsync();
        _seed = await E2EHelpers.SeedAuthedSessionAsync(_context);
        _page = await _context.NewPageAsync();
        await E2EHelpers.InstallShellMocksAsync(_page);

        // Page-specific mocks (registered after the shell mocks so they win).
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/identity/roles*", E2EHelpers.Paged(Roles, 1));
        await _page.RouteAsync("**/api/v1/identity/users/search*", route => SearchRouteAsync(route));
        return _page;
    }

    private static async Task SearchRouteAsync(IRoute route)
    {
        if (route.Request.Method == "OPTIONS")
        {
            await E2EHelpers.RouteJsonAsync(route, new { });
            return;
        }

        var query = new Uri(route.Request.Url).Query;
        var searchParam = query.Split('&')
            .Select(part => part.Split('=', 2))
            .FirstOrDefault(parts => parts.Length == 2 && parts[0] == "Search");
        var search = searchParam is null
            ? null
            : Uri.UnescapeDataString(searchParam[1].Replace('+', ' '));

        var items = search == "patel" ? new[] { Users[0] } : Users;
        await E2EHelpers.RouteJsonAsync(route, E2EHelpers.Paged(items, items.Length));
    }

    [Fact]
    public async Task UsersList_RendersTable_AndSearchFiltersResults()
    {
        var page = await NewUsersPageAsync();

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/users");
        await E2EHelpers.WaitForBlazorReadyAsync(page);
        await _seed!.DisposeAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Directory" })).ToBeVisibleAsync();
        await Expect(page.GetByText("2 accounts on this tenant.")).ToBeVisibleAsync();
        await Expect(page.GetByText("@bob.patel", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("@dana.lee", new() { Exact = true })).ToBeVisibleAsync();

        // Typing in the search box issues a filtered search request (250 ms debounce).
        await E2EHelpers.FillAndBlurAsync(page.GetByPlaceholder("Search name, username, email…"), "patel");

        await Expect(page.GetByText("@bob.patel", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("@dana.lee", new() { Exact = true })).ToBeHiddenAsync();
    }

    [Fact]
    public async Task UserCreate_Dialog_SubmitsRegister_AndLandsOnDetailPage()
    {
        var page = await NewUsersPageAsync();

        var registerBody = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/api/v1/identity/register", async route =>
        {
            if (route.Request.Method == "POST")
            {
                registerBody.TrySetResult(JsonDocument.Parse(route.Request.PostData ?? "{}").RootElement);
                await E2EHelpers.RouteJsonAsync(
                    route,
                    new { userId = "u-new", message = "Confirmation email queued." });
                return;
            }

            await E2EHelpers.RouteProblemAsync(route, 404, "Not Found", "Unmocked register request.");
        });

        // Detail-page loads for the freshly created user.
        await E2EHelpers.MockJsonAsync(
            page,
            "**/api/v1/identity/users/u-new",
            new
            {
                id = "u-new",
                userName = "m.chen",
                firstName = "Mei",
                lastName = "Chen",
                email = "m.chen@root.com",
                isActive = true,
                emailConfirmed = false,
                phoneNumber = (string?)null,
                imageUrl = (string?)null,
                twoFactorEnabled = false,
            });
        await E2EHelpers.MockJsonAsync(page, "**/api/v1/identity/users/u-new/roles", Array.Empty<object>());
        await E2EHelpers.MockJsonAsync(page, "**/api/v1/identity/users/u-new/sessions", Array.Empty<object>());

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/users");
        await E2EHelpers.WaitForBlazorReadyAsync(page);
        await _seed!.DisposeAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Directory" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "New user" }).ClickAsync();
        var dialog = page.Locator(".mud-dialog");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByLabel("First name")).ToBeVisibleAsync();

        await E2EHelpers.FillAndBlurAsync(dialog.GetByLabel("First name"), "Mei");
        await E2EHelpers.FillAndBlurAsync(dialog.GetByLabel("Last name"), "Chen");
        await E2EHelpers.FillAndBlurAsync(dialog.GetByLabel("Username"), "m.chen");
        await E2EHelpers.FillAndBlurAsync(dialog.GetByLabel("Email"), "m.chen@root.com");
        await E2EHelpers.FillAndBlurAsync(dialog.GetByLabel("Password", new() { Exact = true }), "Sup3rSecret!");
        await E2EHelpers.FillAndBlurAsync(dialog.GetByLabel("Confirm password"), "Sup3rSecret!");

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create user" }).ClickAsync();

        var body = await registerBody.Task.WaitAsync(TimeSpan.FromSeconds(15));
        body.GetProperty("firstName").GetString().ShouldBe("Mei");
        body.GetProperty("lastName").GetString().ShouldBe("Chen");
        body.GetProperty("userName").GetString().ShouldBe("m.chen");
        body.GetProperty("email").GetString().ShouldBe("m.chen@root.com");
        body.GetProperty("password").GetString().ShouldBe("Sup3rSecret!");
        body.GetProperty("confirmPassword").GetString().ShouldBe("Sup3rSecret!");

        // The create handler navigates to the new user's detail page.
        await page.WaitForURLAsync("**/users/u-new", new() { Timeout = 15_000 });
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Mei Chen" }).First).ToBeVisibleAsync();
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
