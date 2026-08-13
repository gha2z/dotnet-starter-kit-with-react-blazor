using Bunit;
using FSH.Admin.Wasm.Pages.Webhooks;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Webhooks;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Webhooks;

public class WebhooksListPageTests : TestSetup
{
    private readonly IWebhookService _webhookService = Substitute.For<IWebhookService>();

    public WebhooksListPageTests()
    {
        Services.AddSingleton(_webhookService);
    }

    private static WebhookSubscriptionDto SampleSubscription(
        Guid id,
        string url,
        params string[] events) =>
        new(id, url, events, IsActive: true, new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc));

    private static PagedResult<WebhookSubscriptionDto> Page(params WebhookSubscriptionDto[] subscriptions) =>
        new([.. subscriptions], 1, 25, subscriptions.Length, subscriptions.Length == 0 ? 0 : 1, false, false);

    private void StubList(params WebhookSubscriptionDto[] subscriptions)
    {
        _webhookService.GetSubscriptionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Page(subscriptions));
    }

    [Fact]
    public void Renders_subscriptions_with_url_events_and_status()
    {
        StubList(SampleSubscription(
            Guid.NewGuid(),
            "https://api.acme.com/webhooks/fsh",
            "user.registered",
            "billing.invoice.issued"));

        var cut = Render<WebhooksListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("https://api.acme.com/webhooks/fsh"));
        cut.Markup.ShouldContain("user.registered");
        cut.Markup.ShouldContain("billing.invoice.issued");
        cut.Markup.ShouldContain("Active");
    }

    [Fact]
    public void Shows_empty_state_when_no_subscriptions()
    {
        StubList();

        var cut = Render<WebhooksListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No webhook subscriptions yet."));
        cut.Markup.ShouldNotContain("route-not-found");
    }

    [Fact]
    public void Shows_error_band_when_load_fails()
    {
        _webhookService.GetSubscriptionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<WebhookSubscriptionDto>>(new Exception("boom")));

        var cut = Render<WebhooksListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load subscriptions"));
    }

    [Fact]
    public void New_subscription_button_hidden_without_create_permission()
    {
        StubList();

        var cut = Render<WebhooksListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No webhook subscriptions yet."));

        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("New subscription"));
    }

    [Fact]
    public void New_subscription_button_shown_with_create_permission()
    {
        StubList();
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(WebhooksPermissions.Subscriptions.Create);

        var cut = Render<WebhooksListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No webhook subscriptions yet."));

        cut.FindAll("button").ShouldContain(b => b.TextContent.Contains("New subscription"));
    }

    [Fact]
    public async Task Test_button_sends_test_event_and_reports_success()
    {
        var subscription = SampleSubscription(Guid.NewGuid(), "https://api.acme.com/webhooks/fsh", "user.registered");
        StubList(subscription);
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(WebhooksPermissions.Subscriptions.Test);
        _webhookService.TestSubscriptionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var cut = Render<WebhooksListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("https://api.acme.com/webhooks/fsh"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Test")).Click();

        await cut.WaitForAssertionAsync(() =>
            _webhookService.Received(1).TestSubscriptionAsync(subscription.Id, Arg.Any<CancellationToken>()));
    }
}
