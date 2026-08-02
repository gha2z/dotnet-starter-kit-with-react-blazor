using Bunit;
using FSH.Admin.Wasm.Pages.Billing;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Billing;

public class PlansListPageTests : TestSetup
{
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();

    public PlansListPageTests()
    {
        Services.AddSingleton(_billingService);
    }

    private static BillingPlanDto SamplePlan(
        Guid id,
        string key,
        string name,
        decimal monthlyBasePrice,
        bool isActive = true,
        string interval = "Monthly",
        decimal? annualPrice = null,
        Dictionary<string, decimal>? overage = null) =>
        new(id, key, name, "USD", monthlyBasePrice, overage ?? new Dictionary<string, decimal>(), isActive, interval, annualPrice);

    [Fact]
    public void Renders_plans_with_kpis_and_pricing()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([
                SamplePlan(Guid.NewGuid(), "free", "Free", 0m, isActive: false),
                SamplePlan(Guid.NewGuid(), "pro", "Pro", 49m, interval: "Yearly", annualPrice: 490m, overage: new Dictionary<string, decimal> { ["Users"] = 5m }),
                SamplePlan(Guid.NewGuid(), "team", "Team", 19m),
            ]);

        var cut = Render<PlansListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Free"));
        cut.Markup.ShouldContain("Pro");
        cut.Markup.ShouldContain("Team");
        cut.Markup.ShouldContain("2 active");
        cut.Markup.ShouldContain("1 inactive");
        cut.Markup.ShouldContain("USD 22.67");
        cut.Markup.ShouldContain("USD 0.00");
        cut.Markup.ShouldContain("per month");
        cut.Markup.ShouldContain("USD 490.00");
        cut.Markup.ShouldContain("per year");
        cut.Markup.ShouldContain("Users USD 5.00");
        cut.Markup.ShouldContain("overage —");
    }

    [Fact]
    public void Empty_state_when_no_plans()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<PlansListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No plans defined yet."));
        cut.Markup.ShouldContain("Create your first plan to start charging tenants.");
    }

    [Fact]
    public void Load_failure_shows_error_band()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<BillingPlanDto>>(new InvalidOperationException("boom")));

        var cut = Render<PlansListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load plans: boom"));
    }

    [Fact]
    public void New_plan_button_is_gated_by_manage_permission()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([]);

        var unauthorized = Render<PlansListPage>();
        unauthorized.WaitForAssertion(() => unauthorized.Markup.ShouldContain("No plans defined yet."));
        unauthorized.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("New plan"));

        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<PlansListPage>(1);
            builder.CloseComponent();
        });
        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("New plan")));

        cut.FindAll("button").First(b => b.TextContent.Contains("New plan")).Click();

        cut.WaitForAssertion(
            () => cut.FindAll("button").Any(b => b.TextContent.Contains("Create plan")));
        cut.Markup.ShouldContain("Plan details");
    }

    [Fact]
    public void Edit_button_opens_dialog_prefilled_with_plan()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan(Guid.NewGuid(), "pro", "Pro", 49m)]);

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<PlansListPage>(1);
            builder.CloseComponent();
        });
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Pro"));

        cut.FindAll("button.mud-icon-button").First().Click();

        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("Save changes")));
        cut.Markup.ShouldContain("Save changes");
        cut.FindAll("input").Any(i => i.GetAttribute("value") == "Pro").ShouldBeTrue();
        cut.FindAll("input").Any(i => i.GetAttribute("value") == "49").ShouldBeTrue();
    }
}
