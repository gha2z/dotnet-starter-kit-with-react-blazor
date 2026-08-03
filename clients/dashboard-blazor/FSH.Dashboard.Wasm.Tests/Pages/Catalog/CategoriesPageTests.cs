using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Catalog;

public sealed class CategoriesPageTests : TestSetup
{
    private readonly ICatalogService _catalog = Substitute.For<ICatalogService>();

    public CategoriesPageTests()
    {
        Services.AddSingleton(_catalog);
    }

    private static CategoryDto SampleCategory(string name = "Outdoor", string slug = "outdoor", Guid? parentId = null) =>
        new(Guid.NewGuid(), name, slug, "Gear for the outdoors", parentId, DateTime.UtcNow.AddDays(-10), null);

    [Fact]
    public void Renders_categories_with_name_and_slug()
    {
        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>())
            .Returns(new List<CategoryTreeNodeDto>());

        _catalog.SearchCategoriesAsync(null, null, 1, 50, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>(
                [SampleCategory("Outdoor", "outdoor"), SampleCategory("Indoor", "indoor")],
                1, 50, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.CategoriesPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Categories");
            cut.Markup.ShouldContain("Outdoor");
            cut.Markup.ShouldContain("Indoor");
            cut.Markup.ShouldContain("outdoor");
            cut.Markup.ShouldContain("indoor");
        });
    }

    [Fact]
    public void Renders_empty_state_when_no_categories()
    {
        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>())
            .Returns(new List<CategoryTreeNodeDto>());

        _catalog.SearchCategoriesAsync(null, null, 1, 50, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 50, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.CategoriesPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No categories yet"));
    }

    [Fact]
    public void Renders_error_band_on_failure()
    {
        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>())
            .Returns(new List<CategoryTreeNodeDto>());

        _catalog.SearchCategoriesAsync(null, null, 1, 50, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<CategoryDto>>(new InvalidOperationException("boom")));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.CategoriesPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Search_filters_categories_client_side()
    {
        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>())
            .Returns(new List<CategoryTreeNodeDto>());

        _catalog.SearchCategoriesAsync(null, null, 1, 50, "name", "asc", Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>(
                [SampleCategory("Outdoor", "outdoor"), SampleCategory("Indoor", "indoor")],
                1, 50, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Catalog.CategoriesPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Outdoor"));

        var searchInput = cut.Find("input");
        searchInput.Input("Outdoor");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Outdoor");
            cut.Markup.ShouldNotContain("Indoor");
        });
    }
}
