using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Identity;

/// <summary>
/// Regression coverage for the route-binding crash: detail routes previously used
/// <c>{Id:guid}</c> constraints while pages bind <c>[Parameter] string Id</c> — the
/// constrained route value is a Guid object, which cannot be cast to string
/// ("Unable to set property 'Id' ... Arg_InvalidCastException").
/// These tests exercise the real Router + route parameter binding, not direct param passing.
/// </summary>
public sealed class RouteBindingRegressionTests : TestSetup
{
    private const string UserId = "11111111-1111-1111-1111-111111111111";
    private const string RoleId = "22222222-2222-2222-2222-222222222222";

    /// <summary>
    /// Minimal layout for the routed page. LayoutComponentBase in .NET 10 does not
    /// auto-render Body anymore, so a pure C# layout must render it explicitly.
    /// </summary>
    public sealed class RouteHost : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }

    private static RenderFragment<RouteData> FoundFragment() =>
        routeData => builder =>
        {
            builder.OpenComponent<RouteView>(0);
            builder.AddAttribute(1, nameof(RouteView.RouteData), routeData);
            builder.AddAttribute(2, nameof(RouteView.DefaultLayout), typeof(RouteHost));
            builder.CloseComponent();
        };

    private static RenderFragment NotFoundFragment() =>
        builder =>
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "route-not-found");
            builder.CloseElement();
        };

    /// <summary>
    /// Navigates before rendering so the Router's initial match targets the page under
    /// test — the app's "/" overview route has its own service dependencies and is not
    /// part of what these tests assert.
    /// </summary>
    private IRenderedComponent<Router> RenderRouter(string route)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(route);

        return Render<Router>(p => p
            .Add(x => x.AppAssembly, typeof(FSH.Admin.Wasm.App).Assembly)
            .Add(x => x.Found, FoundFragment())
#pragma warning disable CS0618 // Router.NotFound is deprecated but lets the test assert arbitrary markup without a routed page type.
            .Add(x => x.NotFound, NotFoundFragment()));
#pragma warning restore CS0618
    }

    [Fact]
    public void Navigating_to_users_detail_route_binds_string_id()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(UserId, "janedoe", "Jane", "Doe", "jane@example.com", true, true, null, null, false));
        userService.GetRolesAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);
        userService.GetSessionsAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);
        Services.AddSingleton(userService);

        var router = RenderRouter($"/users/{UserId}");

        router.WaitForAssertion(() => router.Markup.ShouldContain("jane@example.com"));
        router.Markup.ShouldNotContain("route-not-found");
    }

    [Fact]
    public void Navigating_to_roles_detail_route_binds_string_id()
    {
        var roleService = Substitute.For<IRoleService>();
        roleService.GetWithPermissionsAsync(RoleId, Arg.Any<CancellationToken>())
            .Returns(new RoleDto(RoleId, "Support", "Support desk", ["Permissions.Users.View"]));
        roleService.GetPermissionCatalogAsync(Arg.Any<CancellationToken>())
            .Returns([
                new PermissionCatalogEntryDto("Permissions.Users.View", "View users", "Users", "View", true, false),
            ]);
        Services.AddSingleton(roleService);

        var router = RenderRouter($"/roles/{RoleId}");

        router.WaitForAssertion(() => router.Markup.ShouldContain("Support"));
        router.Markup.ShouldNotContain("route-not-found");
    }

    [Fact]
    public void Navigating_to_billing_invoice_detail_route_binds_string_id()
    {
        var billingService = Substitute.For<IBillingService>();
        billingService.GetInvoiceByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new InvoiceDto(
                Guid.Parse(RoleId),
                "acme-corp",
                "INV-2026-07-001",
                2026,
                7,
                "USD",
                315m,
                "Draft",
                new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
                null,
                null,
                null,
                null,
                null,
                [],
                "Usage",
                null,
                null));
        Services.AddSingleton(billingService);

        var router = RenderRouter($"/billing/invoices/{RoleId}");

        router.WaitForAssertion(() => router.Markup.ShouldContain("INV-2026-07-001"));
        router.Markup.ShouldNotContain("route-not-found");
    }

    [Fact]
    public void Navigating_to_billing_index_redirects_to_invoices()
    {
        var billingService = Substitute.For<IBillingService>();
        billingService.GetInvoicesAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 20, 0, 1, false, false));
        Services.AddSingleton(billingService);

        var router = RenderRouter("/billing");

        // React parity: /billing redirects (replace) to /billing/invoices.
        router.WaitForAssertion(() => router.Markup.ShouldContain("No invoices found."));
        router.Markup.ShouldNotContain("route-not-found");
    }

    [Fact]
    public void Unknown_route_renders_not_found_template()
    {
        var router = RenderRouter("/definitely-not-a-route");

        router.WaitForAssertion(() => router.Markup.ShouldContain("route-not-found"));
    }
}
