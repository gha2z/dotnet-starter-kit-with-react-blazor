using Bunit;
using FSH.Admin.Wasm.Pages.Tenants;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Tenants;

public class TenantsListPageTests : TestSetup
{
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();

    public TenantsListPageTests()
    {
        Services.AddSingleton(_tenantService);
        Services.AddSingleton(_billingService);
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private static TenantDto SampleTenant(string id, string name, string adminEmail, bool isActive = true) =>
        new()
        {
            Id = id,
            Name = name,
            AdminEmail = adminEmail,
            IsActive = isActive,
            ValidUpto = new DateTime(2027, 1, 15),
        };

    private static PagedResult<TenantDto> Page(params TenantDto[] tenants) =>
        new([.. tenants], 1, 12, tenants.Length, tenants.Length == 0 ? 0 : 1, false, false);

    [Fact]
    public void Renders_tenants_loaded_from_service()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Page(
                SampleTenant("acme-corp", "Acme Corp", "admin@acme.example"),
                SampleTenant("globex", "Globex", "admin@globex.example", isActive: false)));

        var cut = Render<TenantsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Acme Corp"));
        cut.Markup.ShouldContain("acme-corp · valid Jan 15, 2027");
        cut.Markup.ShouldContain("admin@acme.example");
        cut.Markup.ShouldContain("admin@globex.example");
        cut.Markup.ShouldContain("2 tenants registered on this instance.");
        cut.FindAll("tbody tr.mud-table-row").Count.ShouldBe(2);
    }

    [Fact]
    public void Row_click_navigates_to_tenant_detail()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Page(SampleTenant("acme-corp", "Acme Corp", "admin@acme.example")));

        var cut = Render<TenantsListPage>();
        cut.WaitForAssertion(() => cut.FindAll("tbody tr.mud-table-row").Count.ShouldBe(1));

        // The click can race the renderer replacing the row's event handler.
        // Re-find the row on every attempt (bUnit's documented workaround for
        // UnknownEventHandlerIdException) and poll until navigation happens.
        cut.WaitForAssertion(() =>
        {
            cut.Find("tbody tr.mud-table-row").Click();
            var nav = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
            nav.History.Last().Uri.ShouldEndWith("/tenants/acme-corp");
        });
    }

    [Fact]
    public void Empty_registry_shows_empty_state()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Page());

        var cut = Render<TenantsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No tenants yet."));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Provision the first tenant to get started."));
    }

    [Fact]
    public void Load_failure_shows_alert()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<TenantDto>>(new InvalidOperationException("nope")));

        var cut = Render<TenantsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load tenants: nope"));
    }

    [Fact]
    public void New_tenant_button_hidden_without_permission()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Page());

        var cut = Render<TenantsListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No tenants yet."));

        cut.FindAll("button").Any(b => b.TextContent.Contains("New tenant")).ShouldBeFalse();
    }

    [Fact]
    public void New_tenant_button_opens_create_dialog()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Page());
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(MultitenancyPermissions.Tenants.Create);

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TenantsListPage>(1);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No tenants yet."));

        cut.FindAll("button").First(b => b.TextContent.Contains("New tenant")).Click();

        cut.WaitForAssertion(
            () => cut.FindAll("button").Any(b => b.TextContent.Contains("Create tenant")));
        cut.Markup.ShouldContain("Display name");
    }
}
