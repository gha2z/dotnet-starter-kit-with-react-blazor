using System.Net;
using System.Net.Http.Headers;

namespace FSH.BlazorShared.Infrastructure;

/// <summary>
/// Transparently retries a single 429 (Too Many Requests) after honoring the
/// Retry-After header. Only idempotent verbs (GET/HEAD/PUT/DELETE) are retried —
/// a POST/PATCH must surface the 429 to the caller, which decides how to proceed.
/// Registered innermost (after AuthDelegatingHandler) so it sees the final response.
/// </summary>
public sealed class RetryAfterHandler : DelegatingHandler
{
    /// <summary>Safety cap so a hostile/garbage Retry-After cannot stall the app.</summary>
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private static readonly HashSet<HttpMethod> IdempotentMethods = new(
    [
        HttpMethod.Get,
        HttpMethod.Head,
        HttpMethod.Put,
        HttpMethod.Delete,
    ]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.TooManyRequests || !IdempotentMethods.Contains(request.Method))
        {
            return response;
        }

        var delay = ParseRetryAfter(response.Headers.RetryAfter);
        if (delay is null)
        {
            return response;
        }

        await Task.Delay(delay.Value, cancellationToken);

        // Dispose the 429 before retrying so the connection is returned to the pool.
        response.Dispose();
        return await base.SendAsync(request, cancellationToken);
    }

    private static TimeSpan? ParseRetryAfter(RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter?.Delta is { } delta)
        {
            return delta > MaxRetryDelay ? MaxRetryDelay : delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var remaining = date - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            return remaining > MaxRetryDelay ? MaxRetryDelay : remaining;
        }

        return null;
    }
}
