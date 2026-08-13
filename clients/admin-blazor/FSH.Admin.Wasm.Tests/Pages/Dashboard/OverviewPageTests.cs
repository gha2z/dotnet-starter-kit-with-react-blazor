using Bunit;
using FSH.Admin.Wasm.Pages.Dashboard;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Dashboard;

public class OverviewPageTests : TestSetup
{
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();

    public OverviewPageTests()
    {
        Services.AddSingleton(_tenantService);
        Services.AddSingleton(_billingService);
    }

    private static BillingPlanDto SamplePlan(string key, bool isActive = true) =>
        new(Guid.NewGuid(), key, key, "USD", 10m, new Dictionary<string, decimal>(), isActive, "Monthly", null);

    private static InvoiceDto SampleInvoice(Guid id, string status) =>
        new(id, "root", $"INV-{id:N}", 2026, 7, "USD", 100m, status, DateTime.UtcNow, null, null, null, null, null, [], "Subscription", null, null);

    [Fact]
    public void Renders_all_four_stats_from_services()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TenantDto>([], 1, 1, 12, 12, true, false));
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan("Starter"), SamplePlan("Pro"), SamplePlan("Retired", isActive: false)]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>(
                [SampleInvoice(Guid.NewGuid(), "Issued"), SampleInvoice(Guid.NewGuid(), "Paid")],
                1, 50, 7, 1, false, false));

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Tenants");
            cut.Markup.ShouldContain("registered on this instance");
            cut.Markup.ShouldContain("Plans");
            cut.Markup.ShouldContain("2 active");
            cut.Markup.ShouldContain("Invoices");
            cut.Markup.ShouldContain("7 total ledger");
            cut.Markup.ShouldContain("Outstanding");
            cut.Markup.ShouldContain("issued, awaiting payment");
        });
    }

    [Fact]
    public void Shows_placeholders_until_data_arrives_then_values()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TenantDto>([], 1, 1, 4, 4, false, false));
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan("Starter")]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 50, 0, 0, false, false));

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("4");
            cut.Markup.ShouldContain("1 active");
            cut.Markup.ShouldContain("0 total ledger");
        });
    }

    [Fact]
    public void Shows_error_alert_when_stats_fail_to_load()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<TenantDto>>(new InvalidOperationException("boom")));

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load tenant stats"));
    }

    [Fact]
    public void Other_tiles_still_render_when_one_fetch_fails()
    {
        _tenantService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<TenantDto>>(new InvalidOperationException("boom")));
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan("Starter")]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 50, 0, 0, false, false));

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Failed to load tenant stats");
            cut.Markup.ShouldContain("1 active");
            cut.Markup.ShouldContain("0 total ledger");
        });
    }
}
