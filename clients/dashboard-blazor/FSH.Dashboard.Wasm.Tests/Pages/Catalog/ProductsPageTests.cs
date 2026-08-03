using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Catalog;

public sealed class ProductsPageTests : TestSetup
{
    private readonly ICatalogService _catalog = Substitute.For<ICatalogService>();

    public ProductsPageTests()
    {
        Services.AddSingleton(_catalog);
    }

    private static ProductDto SampleProduct(string name = "Camping Stove", string sku = "CS-001",
        decimal price = 49.99m, string currency = "USD", int stock = 25, bool isActive = true) =>
        new(Guid.NewGuid(), sku, name, "camping-stove", "Lightweight stove",
            Guid.NewGuid(), Guid.NewGuid(), new MoneyDto(price, currency),
            stock, isActive, null, [], DateTime.UtcNow.AddDays(-5), null);

    [Fact]
    public void Renders_products_with_name_sku_and_price()
    {
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchProductsAsync(null, null, null, null, 1, 25, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(
                [SampleProduct("Camping Stove", "CS-001", 49.99m, "USD", 25)],
                1, 25, 1, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductsPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Products");
            cut.Markup.ShouldContain("Camping Stove");
            cut.Markup.ShouldContain("CS-001");
            cut.Markup.ShouldContain("USD 49.99");
        });
    }

    [Fact]
    public void Shows_out_of_stock_when_stock_zero()
    {
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchProductsAsync(null, null, null, null, 1, 25, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(
                [SampleProduct(stock: 0)],
                1, 25, 1, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("out"));
    }

    [Fact]
    public void Shows_low_stock_warning()
    {
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchProductsAsync(null, null, null, null, 1, 25, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(
                [SampleProduct(stock: 5)],
                1, 25, 1, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("5"));
    }

    [Fact]
    public void Renders_empty_state_when_no_products()
    {
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchProductsAsync(null, null, null, null, 1, 25, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>([], 1, 25, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No products yet"));
    }

    [Fact]
    public void Renders_error_band_on_failure()
    {
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchProductsAsync(null, null, null, null, 1, 25, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<ProductDto>>(new InvalidOperationException("boom")));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Search_filters_products_client_side()
    {
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchProductsAsync(null, null, null, null, 1, 25, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(
                [SampleProduct("Camping Stove", "CS-001"), SampleProduct("Lantern", "LT-001")],
                1, 25, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductsPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Camping Stove"));

        var searchInput = cut.Find("input");
        searchInput.Input("Lantern");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Lantern");
            cut.Markup.ShouldNotContain("Camping Stove");
        });
    }

    [Fact]
    public void Shows_hidden_badge_when_not_active()
    {
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchProductsAsync(null, null, null, null, 1, 25, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(
                [SampleProduct(isActive: false)],
                1, 25, 1, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("hidden"));
    }
}
