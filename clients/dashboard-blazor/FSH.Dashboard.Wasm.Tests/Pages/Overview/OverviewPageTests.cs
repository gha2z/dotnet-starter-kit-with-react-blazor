using Bunit;
using Bunit.TestDoubles;
using FSH.BlazorShared.Models.Audits;
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
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly ISseService _sseService = new FakeSseService();

    public OverviewPageTests()
    {
        Services.AddSingleton(_dashboardService);
        Services.AddSingleton(_sseService);
    }

    private static TenantStatusDto SampleTenantStatus() =>
        new("root", "Acme Corp", true, DateTime.UtcNow.AddDays(30).ToString("O"), "Active", "2026-09-01", false, "admin@acme.test", null, "Pro");

    private static SubscriptionDto SampleSubscription() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Pro", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20), "Active");

    private static UsageSnapshotDto SampleUsage(string resource, long used, long limit) =>
        new(Guid.NewGuid(), "root", DateTime.UtcNow.Year, DateTime.UtcNow.Month, resource, used, limit, 0, DateTime.UtcNow);

    private static AuditSummaryDto SampleAudit(AuditEventType eventType) =>
        new() { Id = Guid.NewGuid(), EventType = eventType, Source = "FSH.Modules.Billing", OccurredAtUtc = DateTime.UtcNow };

    [Fact]
    public void Renders_all_four_react_parity_stats_from_services()
    {
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>())
            .Returns([SampleUsage("ApiCalls", 100, 1000), SampleUsage("StorageBytes", 50, 500)]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleAudit(AuditEventType.EntityChange), SampleAudit(AuditEventType.Security)]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Plan");
            cut.Markup.ShouldContain("Pro");
            cut.Markup.ShouldContain("Active");
            cut.Markup.ShouldContain("Valid for");
            cut.Markup.ShouldContain("until ");
            cut.Markup.ShouldContain("Resources");
            cut.Markup.ShouldContain("2");
            cut.Markup.ShouldContain("% avg utilization");
            cut.Markup.ShouldContain("Live events");
            cut.Markup.ShouldContain("connected");
            cut.Markup.ShouldContain("Acme Corp");
            cut.Markup.ShouldContain("Invite users");
            cut.Markup.ShouldContain("Browse catalog");
        });
    }

    [Fact]
    public void Shows_placeholder_until_data_arrives_then_values()
    {
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Pro");
            cut.Markup.ShouldContain("No usage captured yet");
            cut.Markup.ShouldContain("No recent activity");
            cut.Markup.ShouldContain("Listening for activity");
        });
    }

    [Fact]
    public void Shows_error_band_when_tenant_status_fails()
    {
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
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<UsageSnapshotDto>>(new InvalidOperationException("boom")));
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Failed to load usage data");
            cut.Markup.ShouldContain("Pro");
            cut.Markup.ShouldContain("Acme Corp");
            cut.Markup.ShouldContain("Plan");
        });
    }

    [Fact]
    public void No_subscription_shows_view_plans_and_navigates_to_subscription()
    {
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns((SubscriptionDto?)null);
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No active subscription"));
        cut.FindAll("button").First(b => b.TextContent.Contains("View plans")).Click();

        Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith("/subscription");
    }

    [Fact]
    public void Shows_live_chip_when_sse_connected()
    {
        _dashboardService.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleTenantStatus());
        _dashboardService.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboardService.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _dashboardService.GetRecentAuditsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<OverviewPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("LIVE");
            cut.Markup.ShouldContain("Stream live");
        });
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