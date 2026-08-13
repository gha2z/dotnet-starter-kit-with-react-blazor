using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Dashboard;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Subscription;

public sealed class SubscriptionPageTests : TestSetup
{
    private readonly IDashboardService _dashboard = Substitute.For<IDashboardService>();
    private readonly IBillingService _billing = Substitute.For<IBillingService>();

    public SubscriptionPageTests()
    {
        Services.AddSingleton(_dashboard);
        Services.AddSingleton(_billing);
    }

    private static TenantStatusDto SampleStatus(string expiryState = "Active", bool isActive = true, string? plan = "Pro") =>
        new("acme", "Acme Corp", isActive, "2027-01-01", expiryState, "2026-10-01", false, "admin@acme.test", null, plan);

    private static SubscriptionDto SampleSubscription(string status = "Active") =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Pro", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, status);

    private static UsageSnapshotDto SampleUsage(string resource, long used, long limit, long overage = 0) =>
        new(Guid.NewGuid(), "acme", DateTime.UtcNow.Year, DateTime.UtcNow.Month, resource, used, limit, overage, DateTime.UtcNow);

    private static InvoiceDto SampleInvoice(string status = "Paid") =>
        new(Guid.NewGuid(), "acme", "INV-2026-001", 2026, 7, "USD", 100m, status, DateTime.UtcNow, null, null, null, null, null, [], "Subscription", null, null);

    [Fact]
    public void Renders_plan_and_validity_and_usage_and_invoices()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleStatus());
        _dashboard.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboard.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>())
            .Returns([SampleUsage("ApiCalls", 100, 1000), SampleUsage("StorageBytes", 60, 100, overage: 5)]);
        _billing.GetInvoicesAsync(1, 5, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>(
                [SampleInvoice("Paid"), SampleInvoice("Issued")],
                1, 5, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Subscription.SubscriptionPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Subscription");
            cut.Markup.ShouldContain("Pro");
            cut.Markup.ShouldContain("Active");
            cut.Markup.ShouldContain("ApiCalls");
            cut.Markup.ShouldContain("StorageBytes");
            cut.Markup.ShouldContain("INV-2026-001");
            cut.Markup.ShouldContain("USD 100.00");
            cut.Markup.ShouldContain("2026-07");
        });
    }

    [Fact]
    public void Renders_no_active_subscription_state_when_both_null()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleStatus(plan: null));
        _dashboard.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns((SubscriptionDto?)null);
        _dashboard.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _billing.GetInvoicesAsync(1, 5, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 5, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Subscription.SubscriptionPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No active subscription"));
    }

    [Fact]
    public void Renders_usage_empty_state_when_no_current_month_snapshots()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleStatus());
        _dashboard.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboard.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _billing.GetInvoicesAsync(1, 5, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 5, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Subscription.SubscriptionPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No usage captured yet"));
    }

    [Fact]
    public void Renders_in_grace_expiry_state_with_grace_date()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(SampleStatus(expiryState: "InGrace"));
        _dashboard.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboard.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _billing.GetInvoicesAsync(1, 5, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 5, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Subscription.SubscriptionPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("In grace");
            cut.Markup.ShouldContain("Grace ends");
        });
    }

    [Fact]
    public void Renders_error_band_when_status_fails()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<TenantStatusDto>(new InvalidOperationException("boom")));
        _dashboard.GetMySubscriptionAsync(Arg.Any<CancellationToken>()).Returns(SampleSubscription());
        _dashboard.GetUsageSnapshotsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _billing.GetInvoicesAsync(1, 5, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 5, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Subscription.SubscriptionPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }
}
