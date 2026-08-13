using Bunit;
using FSH.Admin.Wasm.Pages.Webhooks;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Webhooks;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Webhooks;

public class WebhookDetailPageTests : TestSetup
{
    private readonly IWebhookService _webhookService = Substitute.For<IWebhookService>();

    public WebhookDetailPageTests()
    {
        Services.AddSingleton(_webhookService);
    }

    private static WebhookSubscriptionDto SampleSubscription(Guid id, params string[] events) =>
        new(id, "https://api.acme.com/webhooks/fsh", events, IsActive: true, new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc));

    private static WebhookDeliveryDto SampleDelivery(Guid id, Guid subscriptionId, bool success, string? error = null) =>
        new(id, subscriptionId, "user.registered", success ? 200 : 500, success, 1, new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc), error);

    private void StubSubscription(Guid id, params string[] events)
    {
        var sub = SampleSubscription(id, events);
        _webhookService.GetSubscriptionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<WebhookSubscriptionDto>([sub], 1, 200, 1, 1, false, false));
    }

    private void StubDeliveries(Guid subscriptionId, params WebhookDeliveryDto[] deliveries) =>
        _webhookService.GetDeliveriesAsync(subscriptionId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<WebhookDeliveryDto>([.. deliveries], 1, 25, deliveries.Length, deliveries.Length == 0 ? 0 : 1, false, false));

    [Fact]
    public void Renders_subscription_endpoint_events_and_deliveries()
    {
        var id = Guid.NewGuid();
        StubSubscription(id, "user.registered", "billing.invoice.issued");
        StubDeliveries(id, SampleDelivery(Guid.NewGuid(), id, true), SampleDelivery(Guid.NewGuid(), id, false, "connection refused"));

        var cut = Render<WebhookDetailPage>(p => p.Add(x => x.Id, id.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("https://api.acme.com/webhooks/fsh"));
        cut.Markup.ShouldContain("user.registered");
        cut.Markup.ShouldContain("billing.invoice.issued");
        cut.Markup.ShouldContain("Subscribed to 2 events");
        cut.Markup.ShouldContain("2 attempts");
        cut.Markup.ShouldContain("HTTP 200");
        cut.Markup.ShouldContain("HTTP 500");
        cut.Markup.ShouldContain("connection refused");
    }

    [Fact]
    public void Shows_empty_deliveries_message_when_none()
    {
        var id = Guid.NewGuid();
        StubSubscription(id, "user.registered");
        StubDeliveries(id);

        var cut = Render<WebhookDetailPage>(p => p.Add(x => x.Id, id.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No deliveries yet."));
        cut.Markup.ShouldContain("Subscribed to 1 event");
    }

    [Fact]
    public void Shows_not_found_when_subscription_missing()
    {
        _webhookService.GetSubscriptionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<WebhookSubscriptionDto>([], 1, 200, 0, 0, false, false));

        var cut = Render<WebhookDetailPage>(p => p.Add(x => x.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Subscription not found."));
    }

    [Fact]
    public void Shows_error_band_when_load_fails()
    {
        _webhookService.GetSubscriptionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<WebhookSubscriptionDto>>(new Exception("boom")));

        var cut = Render<WebhookDetailPage>(p => p.Add(x => x.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load subscription"));
    }
}
