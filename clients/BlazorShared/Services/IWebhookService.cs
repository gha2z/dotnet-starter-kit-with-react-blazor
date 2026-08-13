using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Webhooks;

namespace FSH.BlazorShared.Services;

public interface IWebhookService
{
    Task<PagedResult<WebhookSubscriptionDto>> GetSubscriptionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default);
    Task<Guid> CreateSubscriptionAsync(CreateWebhookSubscriptionRequest request, CancellationToken ct = default);
    Task DeleteSubscriptionAsync(Guid id, CancellationToken ct = default);
    Task<bool> TestSubscriptionAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<WebhookDeliveryDto>> GetDeliveriesAsync(
        Guid subscriptionId,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default);
}
