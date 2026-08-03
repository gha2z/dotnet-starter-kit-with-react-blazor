using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Webhooks;

namespace FSH.BlazorShared.Services;

public sealed class WebhookService(HttpClient http) : IWebhookService
{
    private const string WebhooksBase = "/api/v1/webhooks";

    public async Task<PagedResult<WebhookSubscriptionDto>> GetSubscriptionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<PagedResult<WebhookSubscriptionDto>>(
            $"{WebhooksBase}/subscriptions?pageNumber={pageNumber}&pageSize={pageSize}", ct)
            ?? EmptyPage<WebhookSubscriptionDto>(pageSize);
    }

    public async Task<Guid> CreateSubscriptionAsync(CreateWebhookSubscriptionRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{WebhooksBase}/subscriptions", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task DeleteSubscriptionAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{WebhooksBase}/subscriptions/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> TestSubscriptionAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{WebhooksBase}/subscriptions/{id}/test", content: null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TestResult>(ct);
        return result?.Success ?? false;
    }

    public async Task<PagedResult<WebhookDeliveryDto>> GetDeliveriesAsync(
        Guid subscriptionId,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<PagedResult<WebhookDeliveryDto>>(
            $"{WebhooksBase}/subscriptions/{subscriptionId}/deliveries?pageNumber={pageNumber}&pageSize={pageSize}", ct)
            ?? EmptyPage<WebhookDeliveryDto>(pageSize);
    }

    private sealed record TestResult(bool Success);

    private static PagedResult<T> EmptyPage<T>(int pageSize) =>
        new([], 1, pageSize, 0, 0, false, false);
}
