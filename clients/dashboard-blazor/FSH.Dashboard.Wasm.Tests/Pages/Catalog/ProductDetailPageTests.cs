using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Catalog;

public sealed class ProductDetailPageTests : TestSetup
{
    private readonly ICatalogService _catalog = Substitute.For<ICatalogService>();

    public ProductDetailPageTests()
    {
        Services.AddSingleton(_catalog);
    }

    private static ProductDto SampleProduct(Guid? id = null, string name = "Camping Stove", string sku = "CS-001",
        decimal price = 49.99m, int stock = 25, bool isActive = true, Guid? brandId = null, Guid? categoryId = null) =>
        new(id ?? Guid.NewGuid(), sku, name, "camping-stove", "Lightweight stove",
            brandId ?? Guid.NewGuid(), categoryId ?? Guid.NewGuid(), new MoneyDto(price, "USD"),
            stock, isActive, null, [], DateTime.UtcNow.AddDays(-5), null);

    [Fact]
    public void Renders_product_header_and_pricing()
    {
        var productId = Guid.NewGuid();
        _catalog.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(SampleProduct(productId, "Lantern", "LN-001", 32.50m, 10));
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, productId.ToString()));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Lantern");
            cut.Markup.ShouldContain("LN-001");
            cut.Markup.ShouldContain("USD 32.50");
            cut.Markup.ShouldContain("Change price");
            cut.Markup.ShouldContain("Adjust stock");
        });
    }

    [Fact]
    public void Shows_active_status_pill()
    {
        var productId = Guid.NewGuid();
        _catalog.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(SampleProduct(productId, isActive: true));
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, productId.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("active"));
    }

    [Fact]
    public void Shows_hidden_status_when_inactive()
    {
        var productId = Guid.NewGuid();
        _catalog.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(SampleProduct(productId, isActive: false));
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, productId.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("hidden"));
    }

    [Fact]
    public void Shows_description()
    {
        var productId = Guid.NewGuid();
        _catalog.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(SampleProduct(productId));
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, productId.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Lightweight stove"));
    }

    [Fact]
    public void Renders_error_band_on_failure()
    {
        _catalog.GetProductByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProductDto>(new InvalidOperationException("boom")));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Renders_not_found_when_null()
    {
        _catalog.GetProductByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProductDto>(null!));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Product not found"));
    }

    [Fact]
    public void Shows_stock_status_inline()
    {
        var productId = Guid.NewGuid();
        _catalog.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(SampleProduct(productId, stock: 0));
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, productId.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("out of stock"));
    }

    [Fact]
    public void Shows_no_images_message_when_images_empty()
    {
        var productId = Guid.NewGuid();
        _catalog.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(SampleProduct(productId));
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, productId.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No images"));
    }

    [Fact]
    public void Shows_images_when_present()
    {
        var productId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var product = SampleProduct(productId);
        product = product with { Images = [new ProductImageDto(imageId, null, "https://img.test/p.jpg", true, 0, DateTime.UtcNow)] };
        _catalog.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);
        _catalog.SearchBrandsAsync(null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 500, 0, 1, false, false));
        _catalog.SearchCategoriesAsync(null, null, 1, 500, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 500, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.ProductDetailPage>(parameters => parameters
            .Add(p => p.Id, productId.ToString()));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("img.test");
            cut.Markup.ShouldContain("cover");
        });
    }
}
