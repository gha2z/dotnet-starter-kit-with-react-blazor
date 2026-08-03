using Bunit;
using Bunit.TestDoubles;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Dashboard;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Sse;
using FSH.Dashboard.Wasm.Pages.Overview;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Overview;

public sealed class OverviewPageTests : TestSetup
{
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly ISseService _sseService = new FakeSseService();

    public OverviewPageTests()
    {
        Services.AddSingleton(_billingService);
        Services.AddSingleton(_dashboardService);
        Services.AddSingleton(_sseService);
    }

    private static BillingPlanDto SamplePlan(string key, bool isActive = true) =>
        new(Guid.NewGuid(), key, key, "USD", 10m, new Dictionary<string, decimal>(), isActive, "Monthly", null);

    private static InvoiceDto SampleInvoice(Guid id, string status) =>
        new(id, "root", $"INV-{id:N}", 2026, 7, "USD", 100m, status, DateTime.UtcNow, null, null, null, null, null, [], "Subscription", null, null);

    private static TenantStatusDto SampleTenantStatus() =>
        new("root", "Acme Corp", true, "2027-01-01", "Active", "2026-09-01", false, "admin@acme.test", null, "Pro");

    private static SubscriptionDto SampleSubscription() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Pro", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20), "Active");

    private static UsageSnapshotDto SampleUsage(string resource, long used, long limit) =>
        new(Guid.NewGuid(), "root", DateTime.UtcNow.Year, DateTime.UtcNow.Month, resource, used, limit, 0, DateTime.UtcNow);

    private static AuditSummaryDto SampleAudit(AuditEventType eventType) =>
        new() { Id = Guid.NewGuid(), EventType = eventType, Source = "FSH.Modules.Billing", OccurredAtUtc = DateTime.UtcNow };

    [Fact]
    public void Renders_all_four_stats_from_services()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan("Starter"), SamplePlan("Pro"), SamplePlan("Retired", isActive: false)]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>(
                [SampleInvoice(Guid.NewGuid(), "Issued"), SampleInvoice(Guid.NewGuid(), "Paid")],
                1, 50, 7, 1, false, false));
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>())
            .Returns([SampleUsage("ApiCalls", 100, 1000), SampleUsage("StorageBytes", 50, 500)]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleAudit(AuditEventType.EntityChange), SampleAudit(AuditEventType.Security)]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Plans");
            cut.Markup.ShouldContain("3");
            cut.Markup.ShouldContain("2 active");
            cut.Markup.ShouldContain("Invoices");
            cut.Markup.ShouldContain("7 total ledger");
            cut.Markup.ShouldContain("Outstanding");
            cut.Markup.ShouldContain("issued, awaiting payment");
            cut.Markup.ShouldContain("Usage");
            cut.Markup.ShouldContain("Acme Corp");
            cut.Markup.ShouldContain("PRO");
        });
    }

    [Fact]
    public void Shows_placeholder_until_data_arrives_then_values()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan("Starter")]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 50, 0, 0, false, false));
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("1 active");
            cut.Markup.ShouldContain("0 total ledger");
            cut.Markup.ShouldContain("No usage data available");
            cut.Markup.ShouldContain("No recent activity");
        });
    }

    [Fact]
    public void Shows_error_band_when_tenant_status_fails()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan("Starter")]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 50, 0, 0, false, false));
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TenantStatusDto>(new InvalidOperationException("boom")));
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load tenant status"));
    }

    [Fact]
    public void Other_widgets_still_render_when_one_fetch_fails()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([SamplePlan("Starter")]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<InvoiceDto>>(new InvalidOperationException("boom")));
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Failed to load invoice stats");
            cut.Markup.ShouldContain("Acme Corp");
            cut.Markup.ShouldContain("PRO");
        });
    }

    [Fact]
    public void No_subscription_shows_view_plans_and_navigates_to_subscription()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([SamplePlan("Starter")]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 50, 0, 0, false, false));
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns((SubscriptionDto?)null);
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No active subscription"));
        cut.FindAll("button").First(b => b.TextContent.Contains("View plans")).Click();

        Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>().Uri.ShouldEndWith("/subscription");
    }

    [Fact]
    public void Shows_live_chip_when_sse_connected()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([SamplePlan("Starter")]);
        _billingService.GetInvoicesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 50, 0, 0, false, false));
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("LIVE"));
    }

    private sealed class FakeSseService : ISseService
    {
        public IObservable<SseEvent> Messages { get; } = new FakeObservable();
        public bool IsConnected => true;
        public event Action? ConnectionChanged;

        public Task StartAsync(CancellationToken ct = default)
        {
            ConnectionChanged?.Invoke();
            return Task.CompletedTask;
        }

        public Task StopAsync() => Task.CompletedTask;

        private sealed class FakeObservable : IObservable<SseEvent>
        {
            public IDisposable Subscribe(IObserver<SseEvent> observer) => new FakeSubscription();

            private sealed class FakeSubscription : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}
