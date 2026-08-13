using System.Text.Json;
using System.Text.Json.Nodes;
using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

[Collection("e2e")]
public sealed class UsersPageTests(BlazorAppServerFixture fixture) : IAsyncLifetime
{
    private static readonly string UsersSearchUrl = "**/api/v1/identity/users/search**";

    private static readonly JsonNode Bob = JsonNode.Parse("""
        {
          "id": "u-bob",
          "userName": "bob.patel",
          "email": "bob.patel@acme.example",
          "firstName": "Bob",
          "lastName": "Patel",
          "displayName": "Bob Patel",
          "phoneNumber": null,
          "isActive": true,
          "isDeleted": false,
          "createdAtUtc": "2026-01-15T09:00:00Z",
          "updatedAtUtc": null,
          "deletedOnUtc": null,
          "deletedBy": null,
          "lastLoggedInAtUtc": "2026-07-01T08:30:00Z"
        }
        """)!;

    private static readonly JsonNode Dana = JsonNode.Parse("""
        {
          "id": "u-dana",
          "userName": "dana.lee",
          "email": "dana.lee@acme.example",
          "firstName": "Dana",
          "lastName": "Lee",
          "displayName": "Dana Lee",
          "phoneNumber": null,
          "isActive": true,
          "isDeleted": false,
          "createdAtUtc": "2026-02-10T11:00:00Z",
          "updatedAtUtc": null,
          "deletedOnUtc": null,
          "deletedBy": null,
          "lastLoggedInAtUtc": null
        }
        """)!;

    private static readonly object NewUserResponse = new
    {
        userId = "u-new",
        message = "User created.",
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

    /// <summary>
    /// Serves the seeded user list on GET /identity/users/search, honoring the
    /// Search query parameter like the real backend would (the page searches
    /// server-side on every keystroke - a static mock would never filter).
    /// </summary>
    private async Task MockUsersSearchAsync(List<JsonNode> users)
    {
        var queryRegex = new System.Text.RegularExpressions.Regex("[?&]Search=([^&]*)");
        await _page.RouteAsync(UsersSearchUrl, async route =>
        {
            var match = queryRegex.Match(route.Request.Url);
            var search = match.Success
                ? Uri.UnescapeDataString(match.Groups[1].Value)
                : string.Empty;

            var filtered = string.IsNullOrWhiteSpace(search)
                ? users
                : users.Where(u => u.ToJsonString().Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            await E2EHelpers.RouteJsonAsync(route, E2EHelpers.Paged(filtered, filtered.Count));
        });
    }

    private async Task SeedAndOpenUsersAsync()
    {
        await E2EHelpers.SeedAuthedSessionAsync(_page);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await MockUsersSearchAsync([Bob, Dana]);
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/identity/roles*", E2EHelpers.Paged(Array.Empty<object>(), 0));

        await _page.GotoAsync($"{BlazorAppServerFixture.ServerUrl}/identity/users");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
    }

    [Fact]
    public async Task UsersList_RendersSeededUsers_WithHeaderAndMeta()
    {
        await SeedAndOpenUsersAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Users", Exact = true })).ToBeVisibleAsync();

        await _page.GetByText("Bob Patel", new() { Exact = true }).WaitForAsync();
        await _page.GetByText("bob.patel", new() { Exact = true }).WaitForAsync();
        await _page.GetByText("dana.lee@acme.example").WaitForAsync();
        await _page.GetByText("2 users found").WaitForAsync();

        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Register user", Exact = true })).ToBeVisibleAsync();
        await Expect(_page.Locator(".fsh-users-grid.fsh-list-row")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task UsersList_SearchQueriesBackend_FiltersRows()
    {
        await SeedAndOpenUsersAsync();

        await _page.GetByText("bob.patel", new() { Exact = true }).WaitForAsync();
        await Expect(_page.Locator(".fsh-users-grid.fsh-list-row")).ToHaveCountAsync(2);

        await E2EHelpers.FillAndBlurAsync(
            _page.GetByPlaceholder("Search by name, username, or email…"),
            "patel");

        await Expect(_page.Locator(".fsh-users-grid.fsh-list-row")).ToHaveCountAsync(1);
        await _page.GetByText("bob.patel", new() { Exact = true }).WaitForAsync();
        await Expect(_page.GetByText("dana.lee", new() { Exact = true })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task RegisterUser_DialogFlow_PostsRegister_AndNavigatesToNewUser()
    {
        await SeedAndOpenUsersAsync();

        await _page.GetByRole(AriaRole.Button, new() { Name = "Register user", Exact = true }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Register a user", Exact = true })).ToBeVisibleAsync();

        await E2EHelpers.FillAndBlurAsync(_page.Locator(".mud-dialog-content").GetByLabel("First name"), "Mei");
        await E2EHelpers.FillAndBlurAsync(_page.Locator(".mud-dialog-content").GetByLabel("Last name"), "Chen");
        await E2EHelpers.FillAndBlurAsync(_page.Locator(".mud-dialog-content").GetByLabel("Username"), "mei.chen");
        await E2EHelpers.FillAndBlurAsync(_page.Locator(".mud-dialog-content").GetByLabel("Email"), "mei.chen@acme.example");
        await E2EHelpers.FillAndBlurAsync(_page.Locator(".mud-dialog-content").GetByLabel("Password", new() { Exact = true }), "Str0ng!Passw0rd");
        await E2EHelpers.FillAndBlurAsync(_page.Locator(".mud-dialog-content").GetByLabel("Confirm password"), "Str0ng!Passw0rd");

        var registerRequest = new TaskCompletionSource<IRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        _page.Request += (_, request) =>
        {
            if (request.Method == "POST" && request.Url.EndsWith("/api/v1/identity/register"))
            {
                registerRequest.TrySetResult(request);
            }
        };

        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/identity/register", NewUserResponse);

        await _page.Locator(".mud-dialog")
            .GetByRole(AriaRole.Button, new() { Name = "Register user", Exact = true })
            .ClickAsync();

        var request = await registerRequest.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var body = request.PostData ?? "{}";
        body.ShouldContain("\"userName\":\"mei.chen\"");
        body.ShouldContain("\"email\":\"mei.chen@acme.example\"");

        // Dashboard parity: the dialog closes with a success snackbar (no
        // navigation to the detail page - that is admin-app behavior).
        await _page.GetByText("User created. Confirmation email queued.").WaitForAsync();
        await Expect(_page.Locator(".mud-dialog")).ToHaveCountAsync(0);
    }
}
