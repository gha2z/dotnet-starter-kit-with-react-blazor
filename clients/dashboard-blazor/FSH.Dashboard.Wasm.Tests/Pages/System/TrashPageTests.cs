using Bunit;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Catalog;
using FSH.BlazorShared.Models.Files;
using FSH.BlazorShared.Models.Tickets;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.SystemPages;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.System;

public sealed class TrashPageTests : TestSetup
{
    private readonly ICatalogService _catalogService = Substitute.For<ICatalogService>();
    private readonly ITicketService _ticketService = Substitute.For<ITicketService>();
    private readonly IFileService _fileService = Substitute.For<IFileService>();
    private readonly IPermissionsProvider _permissions = Substitute.For<IPermissionsProvider>();

    public TrashPageTests()
    {
        Services.AddSingleton(_catalogService);
        Services.AddSingleton(_ticketService);
        Services.AddSingleton(_fileService);
        Services.AddSingleton(_permissions);
        SetupEmptyCatalog();
        _ticketService.ListTrashedTicketsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TicketDto>([], 1, 20, 0, 0, false, false));
        _fileService.ListTrashedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<FileAssetDto>());
    }

    [Fact]
    public void Renders_header()
    {
        _permissions.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { "Permissions.Products.Restore" });

        var cut = Render<TrashPage>();

        cut.Markup.ShouldContain("Trash");
        cut.Markup.ShouldContain("Soft-deleted records");
    }

    [Fact]
    public void Renders_without_error_when_user_has_no_permissions()
    {
        _permissions.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var cut = Render<TrashPage>();

        cut.Markup.ShouldContain("Trash");
    }

    [Fact]
    public void Renders_with_all_tab_permissions()
    {
        _permissions.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                "Permissions.Products.Restore",
                "Permissions.Brands.Restore",
                "Permissions.Categories.Restore",
                "Permissions.Tickets.Restore",
                "Permissions.Files.ViewTrash",
            });

        var cut = Render<TrashPage>();

        cut.Markup.ShouldContain("Trash");
    }

    private void SetupEmptyCatalog()
    {
        _catalogService.ListTrashedBrandsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BrandDto>([], 1, 20, 0, 0, false, false));
        _catalogService.ListTrashedCategoriesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 1, 20, 0, 0, false, false));
        _catalogService.ListTrashedProductsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>([], 1, 20, 0, 0, false, false));
    }
}
