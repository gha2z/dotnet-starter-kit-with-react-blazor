using Bunit;
using FSH.Admin.Wasm.Pages.Billing;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Billing;

public class TopupsListPageTests : TestSetup
{
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();

    public TopupsListPageTests()
    {
        Services.AddSingleton(_billingService);
    }

    private static TopupRequestDto SampleRequest(
        Guid id,
        string status,
        decimal amount,
        Guid? invoiceId = null,
        string tenantId = "acme-corp") =>
        new(
            id,
            tenantId,
            amount,
            "USD",
            null,
            status,
            invoiceId,
            "operator@fsh.io",
            null,
            new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
            null,
            null);

    private static PagedResult<TopupRequestDto> Page(params TopupRequestDto[] requests) =>
        new([.. requests], 1, 20, requests.Length, requests.Length == 0 ? 0 : 1, false, false);

    private void StubList(params TopupRequestDto[] requests)
    {
        _billingService.GetTopupRequestsAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Page(requests));
    }

    [Fact]
    public void Renders_requests_with_kpi_totals_and_status_pills()
    {
        StubList(
            SampleRequest(Guid.NewGuid(), "Pending", 100m),
            SampleRequest(Guid.NewGuid(), "Invoiced", 200m, invoiceId: Guid.NewGuid()),
            SampleRequest(Guid.NewGuid(), "Completed", 150m));

        var cut = Render<TopupsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("USD 100.00"));
        cut.Markup.ShouldContain("USD 200.00");
        cut.Markup.ShouldContain("USD 150.00");
        cut.Markup.ShouldContain("Pending");
        cut.Markup.ShouldContain("Invoiced");
        cut.Markup.ShouldContain("Completed");
        cut.Markup.ShouldContain("USD 450.00");
        cut.Markup.ShouldContain("awaiting decision (this page)");
        cut.Markup.ShouldContain("3 total");
        cut.Markup.ShouldContain("acme-corp");
    }

    [Fact]
    public void Decision_buttons_only_for_pending_requests()
    {
        StubList(
            SampleRequest(Guid.NewGuid(), "Pending", 100m),
            SampleRequest(Guid.NewGuid(), "Invoiced", 200m),
            SampleRequest(Guid.NewGuid(), "Completed", 150m));
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);

        var cut = Render<TopupsListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("USD 100.00"));

        cut.FindAll("button").Count(b => b.TextContent.Contains("Approve")).ShouldBe(1);
        cut.FindAll("button").Count(b => b.TextContent.Contains("Reject")).ShouldBe(1);
    }

    [Fact]
    public void Decision_buttons_hidden_without_manage_permission()
    {
        StubList(SampleRequest(Guid.NewGuid(), "Pending", 100m));

        var cut = Render<TopupsListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("USD 100.00"));

        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Approve"));
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Reject"));
    }

    [Fact]
    public async Task Approve_sends_note_and_reloads()
    {
        var request = SampleRequest(Guid.NewGuid(), "Pending", 100m);
        StubList(request);
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);
        var invoiceId = Guid.NewGuid();
        _billingService.ApproveTopupRequestAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(invoiceId);

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TopupsListPage>(1);
            builder.CloseComponent();
        });
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("USD 100.00"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Approve")).Click();

        cut.WaitForAssertion(
            () => cut.FindAll("button").Any(b => b.TextContent.Contains("Approve & generate invoice")));
        cut.Markup.ShouldContain("An invoice will be generated for the operator to mark paid.");

        cut.Find("input").Change("internal note");
        cut.FindAll("button").First(b => b.TextContent.Contains("Approve & generate invoice")).Click();

        await cut.WaitForAssertionAsync(() =>
            _billingService.Received(1).ApproveTopupRequestAsync(request.Id, "internal note", Arg.Any<CancellationToken>()));
        _ = _billingService.Received(2).GetTopupRequestsAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Empty_state_when_no_requests_match()
    {
        StubList();

        var cut = Render<TopupsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No requests found."));
        cut.Markup.ShouldContain("No top-up requests match the current filters.");
    }

    [Fact]
    public void Load_failure_shows_error_band()
    {
        _billingService.GetTopupRequestsAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<TopupRequestDto>>(new InvalidOperationException("boom")));

        var cut = Render<TopupsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load top-up requests: boom"));
    }
}
