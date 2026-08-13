using Bunit;
using FSH.BlazorShared.Models.Dashboard;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Components;

public sealed class FshExpiryBannerTests : TestSetup
{
    private readonly IDashboardService _dashboard = Substitute.For<IDashboardService>();

    public FshExpiryBannerTests()
    {
        Services.AddSingleton(_dashboard);
    }

    private static TenantStatusDto SampleStatus(string expiryState = "Active", string? validUpto = null, string? graceEndsUtc = null) =>
        new("acme", "Acme Corp", true, validUpto ?? DateTime.UtcNow.AddDays(90).ToString("yyyy-MM-dd"),
            expiryState, graceEndsUtc ?? DateTime.UtcNow.AddDays(95).ToString("yyyy-MM-dd"),
            false, "admin@acme.test", null, "Pro");

    private IRenderedComponent<FSH.Dashboard.Wasm.Shared.FshExpiryBanner> RenderBanner()
        => Render<FSH.Dashboard.Wasm.Shared.FshExpiryBanner>();

    [Fact]
    public void Healthy_active_subscription_renders_no_banner()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns(SampleStatus());

        var cut = RenderBanner();

        cut.FindAll(".mud-alert").ShouldBeEmpty();
    }

    [Fact]
    public void Nearing_expiry_renders_info_banner_with_days_left()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns(SampleStatus(validUpto: DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd")));

        var cut = RenderBanner();

        var alert = cut.FindAll(".mud-alert").ShouldHaveSingleItem();
        alert.TextContent.ShouldContain("expires in");
        alert.TextContent.ShouldContain("3 days");
        cut.Find("button[aria-label='Dismiss subscription notice']").ShouldNotBeNull();
    }

    [Fact]
    public void In_grace_renders_warning_banner_with_days_and_end_date()
    {
        var graceEnd = DateTime.UtcNow.AddDays(5);
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns(SampleStatus(expiryState: "InGrace", graceEndsUtc: graceEnd.ToString("yyyy-MM-dd")));

        var cut = RenderBanner();

        var alert = cut.FindAll(".mud-alert").ShouldHaveSingleItem();
        alert.TextContent.ShouldContain("5 days of grace left");
        alert.TextContent.ShouldContain(graceEnd.ToString("MMM dd, yyyy"));
        alert.TextContent.ShouldContain("Contact your operator to renew");
        cut.Find("button[aria-label='Dismiss subscription notice']").ShouldNotBeNull();
    }

    [Fact]
    public void Expired_renders_error_banner_without_dismiss_action()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns(SampleStatus(expiryState: "Expired"));

        var cut = RenderBanner();

        var alert = cut.FindAll(".mud-alert").ShouldHaveSingleItem();
        alert.TextContent.ShouldContain("has expired");
        alert.TextContent.ShouldContain("restore full access");
        // The expired state stays pinned — no dismiss (React parity).
        cut.FindAll("button[aria-label='Dismiss subscription notice']").ShouldBeEmpty();
    }

    [Fact]
    public void Dismissing_a_nearing_banner_hides_it()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns(SampleStatus(validUpto: DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd")));

        var cut = RenderBanner();
        cut.FindAll(".mud-alert").ShouldHaveSingleItem();

        cut.Find("button[aria-label='Dismiss subscription notice']").Click();

        cut.FindAll(".mud-alert").ShouldBeEmpty();
    }

    [Fact]
    public void Status_lookup_failure_renders_nothing()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TenantStatusDto>(new InvalidOperationException("boom")));

        var cut = RenderBanner();

        cut.FindAll(".mud-alert").ShouldBeEmpty();
    }

    [Fact]
    public void Inactive_tenant_with_no_status_renders_nothing()
    {
        _dashboard.GetMyTenantStatusAsync(Arg.Any<CancellationToken>())
            .Returns((TenantStatusDto)null!);

        var cut = RenderBanner();

        cut.FindAll(".mud-alert").ShouldBeEmpty();
    }
}