using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Catalog;

public sealed class BrandsPageTests : TestSetup
{
    private readonly ICatalogService _catalog = Substitute.For<ICatalogService>();

    public BrandsPageTests()
    {
        Services.AddSingleton(_catalog);
    }

    private static BrandDto SampleBrand(string name = "Acme", string slug = "acme") =>
        new(Guid.NewGuid(), name, slug, "Quality goods", null, DateTime.UtcNow.AddDays(-10), null);

    [Fact]
    public void Renders_brands_with_name_and_slug()
    {
        _catalog.SearchBrandsAsync(null, 1, 20, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>(
                [SampleBrand("Acme", "acme"), SampleBrand("Zenith", "zenith")],
                1, 20, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.BrandsPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Brands");
            cut.Markup.ShouldContain("Acme");
            cut.Markup.ShouldContain("Zenith");
            cut.Markup.ShouldContain("acme");
            cut.Markup.ShouldContain("zenith");
        });
    }

    [Fact]
    public void Renders_empty_state_when_no_brands()
    {
        _catalog.SearchBrandsAsync(null, 1, 20, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 20, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.BrandsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No brands yet"));
    }

    [Fact]
    public void Renders_error_band_on_failure()
    {
        _catalog.SearchBrandsAsync(null, 1, 20, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<BrandDto>>(new InvalidOperationException("boom")));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.BrandsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Search_filters_brands_client_side()
    {
        _catalog.SearchBrandsAsync(null, 1, 20, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>(
                [SampleBrand("Acme", "acme"), SampleBrand("Zenith", "zenith")],
                1, 20, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.BrandsPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Acme"));

        var searchInput = cut.Find("input");
        searchInput.Input("Zen");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Zenith");
            cut.Markup.ShouldNotContain("Acme");
        });
    }

    [Fact]
    public void Shows_count_chips()
    {
        _catalog.SearchBrandsAsync(null, 1, 20, "createdAtUtc", "desc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>(
                [SampleBrand("Acme", "acme")],
                1, 20, 5, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.BrandsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("5 brands"));
    }
}
