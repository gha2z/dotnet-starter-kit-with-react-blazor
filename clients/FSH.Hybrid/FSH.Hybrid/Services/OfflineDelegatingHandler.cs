using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

/// <summary>
/// Thrown when a mutating request was queued for offline replay instead of being sent.
/// Callers can catch it to show a "queued" state; the queue replays transparently later.
/// </summary>
public sealed class OfflineException : Exception
{
    public OfflineException()
        : base("The device is offline; the request was queued and will be retried.")
    {
    }
}

/// <summary>
/// Queues mutating requests (POST/PUT/PATCH/DELETE) when the device is offline instead of
/// failing the caller. GETs and the auth client pass through untouched — replaying reads
/// would be unsafe. Registered OUTERMOST on the "FSH.Api" pipeline.
/// </summary>
public sealed class OfflineDelegatingHandler(
    IOfflineQueueService queue,
    IConnectivityService connectivity,
    ILogger<OfflineDelegatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var isMutating = request.Method.Method is "POST" or "PUT" or "PATCH" or "DELETE";
        if (isMutating && !connectivity.IsOnline)
        {
            await EnqueueAsync(request, cancellationToken);
            throw new OfflineException();
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException) when (isMutating && !connectivity.IsOnline)
        {
            // The network dropped mid-flight — queue and let the caller know.
            await EnqueueAsync(request, cancellationToken);
            throw new OfflineException();
        }
    }

    private async Task EnqueueAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = null;
        var contentType = string.Empty;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType?.MediaType ?? string.Empty;
        }

        await queue.EnqueueAsync(new QueuedRequest(
            Id: null,
            Method: request.Method.Method,
            Url: request.RequestUri?.AbsoluteUri ?? string.Empty,
            Body: body,
            ContentType: contentType,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RetryCount: 0), cancellationToken);
        logger.LogInformation("Queued {Method} {Url} for offline replay", request.Method.Method, request.RequestUri);
    }
}
