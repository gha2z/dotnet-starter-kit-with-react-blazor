using System.Text.Json;
using FSH.Admin.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.E2E.Tests;

/// <summary>
/// Tenant creation: the create dialog (identifier auto-derived from the
/// display name, plan picker fed by the billing plans endpoint) POSTs to
/// /api/v1/tenants/ and the app navigates to the new tenant's detail page.
/// Fully route-mocked.
/// </summary>
[Collection("e2e")]
public sealed class TenantsPageTests(BlazorAppServerFixture fixture) : IAsyncDisposable
{
    private readonly IBrowser _browser = fixture.Browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IAsyncDisposable? _seed;

    private static readonly object[] Plans =
    [
        new
        {
            id = "00000000-0000-0000-0000-000000000001",
            key = "free",
            name = "Free",
            currency = "USD",
            monthlyBasePrice = 0m,
            overageRates = new Dictionary<string, decimal>(),
            isActive = true,
            interval = "Monthly",
            annualPrice = (decimal?)null,
        },
    ];

    private async Task<IPage> NewTenantsPageAsync()
    {
        _context = await _browser.NewContextAsync();
        _seed = await E2EHelpers.SeedAuthedSessionAsync(_context);
        _page = await _context.NewPageAsync();
        await E2EHelpers.InstallShellMocksAsync(_page);

        // Registry list (page-specific, registered before the create-POST mock so it wins for the list URL).
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/tenants/*", E2EHelpers.Paged(Array.Empty<object>(), 0));
        // The dialog preloads active billing plans to populate the plan picker.
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/billing/plans*", Plans);
        return _page;
    }

    [Fact]
    public async Task TenantCreate_Wizard_Completes_AndLandsOnTenantDetail()
    {
        var page = await NewTenantsPageAsync();

        var createBody = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/api/v1/tenants/", async route =>
        {
            if (route.Request.Method == "POST")
            {
                createBody.TrySetResult(JsonDocument.Parse(route.Request.PostData ?? "{}").RootElement);
                await E2EHelpers.RouteJsonAsync(
                    route,
                    new { id = "acme-corp", provisioningCorrelationId = "corr-1", status = "Queued" });
                return;
            }

            await E2EHelpers.RouteProblemAsync(route, 404, "Not Found", "Unmocked tenant request.");
        });

        // Detail-page loads after creation.
        await E2EHelpers.MockJsonAsync(
            page,
            "**/api/v1/tenants/acme-corp/status",
            new
            {
                id = "acme-corp",
                name = "Acme Corp",
                isActive = true,
                validUpto = "2027-01-01T00:00:00Z",
                hasConnectionString = false,
                adminEmail = "admin@acme.example",
                issuer = "acme-corp",
                plan = "free",
                expiryState = "Active",
                graceEndsUtc = "2027-02-01T00:00:00Z",
            });
        await E2EHelpers.MockJsonAsync(
            page,
            "**/api/v1/tenants/acme-corp/provisioning",
            new
            {
                tenantId = "acme-corp",
                status = "Running",
                correlationId = "corr-1",
                currentStep = "SeedingIdentity",
                error = (string?)null,
                createdUtc = "2026-08-05T10:00:00Z",
                startedUtc = "2026-08-05T10:00:01Z",
                completedUtc = (DateTime?)null,
                steps = Array.Empty<object>(),
            });

        await page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/tenants");
        await E2EHelpers.WaitForBlazorReadyAsync(page);
        await _seed!.DisposeAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Registry" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "New tenant" }).ClickAsync();
        var dialog = page.Locator(".mud-dialog");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByLabel("Display name")).ToBeVisibleAsync();

        // Display name drives the identifier + issuer (auto-derived slug).
        await E2EHelpers.FillAndBlurAsync(dialog.Locator("#ct-name"), "Acme Corp");
        await Expect(dialog.Locator("#ct-id")).ToHaveValueAsync("acme-corp");

        await E2EHelpers.FillAndBlurAsync(dialog.Locator("#ct-adminEmail"), "admin@acme.example");
        await E2EHelpers.FillAndBlurAsync(dialog.Locator("#ct-adminPassword"), "Sup3rSecret!");

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create tenant" }).ClickAsync();

        var body = await createBody.Task.WaitAsync(TimeSpan.FromSeconds(15));
        body.GetProperty("id").GetString().ShouldBe("acme-corp");
        body.GetProperty("name").GetString().ShouldBe("Acme Corp");
        body.GetProperty("adminEmail").GetString().ShouldBe("admin@acme.example");
        body.GetProperty("adminPassword").GetString().ShouldBe("Sup3rSecret!");
        body.GetProperty("issuer").GetString().ShouldBe("acme-corp");
        body.GetProperty("planKey").GetString().ShouldBe("free");

        // The create handler navigates to the new tenant's detail page.
        await page.WaitForURLAsync("**/tenants/acme-corp", new() { Timeout = 15_000 });
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Acme Corp" }).First).ToBeVisibleAsync();
        await Expect(page.GetByText("SeedingIdentity")).ToBeVisibleAsync();
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
