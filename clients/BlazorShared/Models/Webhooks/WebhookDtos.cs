namespace FSH.BlazorShared.Models.Webhooks;

/// <summary>
/// Projection of the server's WebhookSubscriptionDto. Events is the flat list of
/// event-type strings this endpoint subscribes to (kebab-case, e.g. "user.registered").
/// </summary>
public sealed record WebhookSubscriptionDto(
    Guid Id,
    string Url,
    string[] Events,
    bool IsActive,
    DateTime CreatedAtUtc);

/// <summary>
/// One recorded delivery attempt for a subscription.
/// </summary>
public sealed record WebhookDeliveryDto(
    Guid Id,
    Guid SubscriptionId,
    string EventType,
    int HttpStatusCode,
    bool Success,
    int AttemptCount,
    DateTime AttemptedAtUtc,
    string? ErrorMessage);

public sealed record CreateWebhookSubscriptionRequest(
    string Url,
    string[] Events,
    string? Secret);

/// <summary>
/// Curated list of event names commonly emitted by FSH modules (React parity:
/// SUGGESTED_EVENT_TYPES). Subscriptions accept arbitrary strings — these just
/// power the chip picker so operators don't have to remember kebab-case names.
/// </summary>
public static class SuggestedEventTypes
{
    public static readonly string[] All =
    [
        "tenant.created",
        "tenant.activation.changed",
        "user.registered",
        "user.role.assigned",
        "billing.invoice.issued",
        "billing.invoice.paid",
        "billing.subscription.created",
        "billing.subscription.cancelled",
    ];
}
