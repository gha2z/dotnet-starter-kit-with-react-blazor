using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

[Collection("e2e")]
public sealed class CatalogProductsTests(BlazorAppServerFixture fixture) : IAsyncLifetime
{
    private const string BrandId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private const string CategoryId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

    private static readonly object AcmeBrand = new
    {
        id = BrandId,
        name = "Acme",
        slug = "acme",
        description = (string?)null,
        logoUrl = (string?)null,
        createdAtUtc = "2026-01-01T00:00:00Z",
        updatedAtUtc = (DateTime?)null,
    };

    private static readonly object AudioCategory = new
    {
        id = CategoryId,
        name = "Audio",
        slug = "audio",
        description = (string?)null,
        parentCategoryId = (string?)null,
        createdAtUtc = "2026-01-01T00:00:00Z",
        updatedAtUtc = (DateTime?)null,
    };

    private static object Product(string id, string sku, string name, decimal price)
        => new
        {
            id,
            sku,
            name,
            slug = name.ToLowerInvariant().Replace(' ', '-'),
            description = (string?)null,
            brandId = BrandId,
            categoryId = CategoryId,
            price = new { amount = price, currency = "USD" },
            stock = 10,
            isActive = true,
            thumbnailUrl = (string?)null,
            images = Array.Empty<object>(),
            createdAtUtc = "2026-01-01T00:00:00Z",
            updatedAtUtc = (DateTime?)null,
        };

    private static readonly object Headphones = Product(
        "11111111-1111-1111-1111-111111111111",
        "SKU-1001",
        "Wireless Headphones",
        129.99m);

    private static readonly object DeskLamp = Product(
        "22222222-2222-2222-2222-222222222222",
        "SKU-2002",
        "Desk Lamp",
        39.50m);

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

    private async Task SeedAndOpenProductsAsync(object[] products, int total)
    {
        await E2EHelpers.SeedAuthedSessionAsync(_page);
        await E2EHelpers.InstallShellMocksAsync(_page);
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/catalog/products*", E2EHelpers.Paged(products, total));
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/catalog/brands*", E2EHelpers.Paged(new[] { AcmeBrand }, 1));
        await E2EHelpers.MockJsonAsync(_page, "**/api/v1/catalog/categories*", E2EHelpers.Paged(new[] { AudioCategory }, 1));

        await _page.GotoAsync($"{BlazorAppServerFixture.ServerUrl}/catalog/products");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
    }

    [Fact]
    public async Task ProductsPage_RendersProducts_AndFiltersClientSide()
    {
        await SeedAndOpenProductsAsync([Headphones, DeskLamp], 2);

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Products", Exact = true })).ToBeVisibleAsync();

        await _page.GetByText("Wireless Headphones", new() { Exact = true }).WaitForAsync();
        await _page.GetByText("Desk Lamp", new() { Exact = true }).WaitForAsync();
        await Expect(_page.Locator(".fsh-products-grid.fsh-list-row").GetByText("Acme", new() { Exact = true })).ToHaveCountAsync(2);
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "New product", Exact = true })).ToBeVisibleAsync();

        await E2EHelpers.FillAndBlurAsync(
            _page.GetByPlaceholder("Search by name, SKU, or slug…"),
            "wireless");

        await _page.GetByText("Wireless Headphones", new() { Exact = true }).WaitForAsync();
        await Expect(_page.Locator(".fsh-products-grid.fsh-list-row")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ProductsPage_EmptyCatalog_ShowsEmptyState()
    {
        await SeedAndOpenProductsAsync([], 0);

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Products", Exact = true })).ToBeVisibleAsync();
        await _page.GetByText("Add your first product to start building the catalog.").WaitForAsync();
    }
}
