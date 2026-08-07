using System.Net;
using System.Net.Http.Headers;
using FSH.BlazorShared.Infrastructure;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Infrastructure;

public sealed class RetryAfterHandlerTests
{
    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public HttpStatusCode Status { get; set; }

        public TimeSpan? RetryAfter { get; set; }

        public DateTimeOffset? RetryAfterDate { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(Status);
            if (RetryAfter is { } delta)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(delta);
            }
            else if (RetryAfterDate is { } date)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(date);
            }

            return Task.FromResult(response);
        }
    }

    private static (RetryAfterHandler Handler, CountingHandler Inner) Create(CountingHandler inner)
        => (new RetryAfterHandler { InnerHandler = inner }, inner);

    private static Task<HttpResponseMessage> SendAsync(RetryAfterHandler handler, HttpMethod method)
        => new HttpMessageInvoker(handler).SendAsync(new HttpRequestMessage(method, "http://localhost/api/v1/anything"), CancellationToken.None);

    [Fact]
    public async Task Idempotent_429_with_RetryAfter_seconds_is_retried_once()
    {
        var inner = new CountingHandler { Status = HttpStatusCode.TooManyRequests, RetryAfter = TimeSpan.FromSeconds(1) };
        var (handler, _) = Create(inner);

        var response = await SendAsync(handler, HttpMethod.Get);

        inner.Calls.ShouldBe(2);
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Idempotent_429_without_RetryAfter_is_not_retried()
    {
        var inner = new CountingHandler { Status = HttpStatusCode.TooManyRequests };
        var (handler, _) = Create(inner);

        var response = await SendAsync(handler, HttpMethod.Get);

        inner.Calls.ShouldBe(1);
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Post_429_is_never_retried()
    {
        var inner = new CountingHandler { Status = HttpStatusCode.TooManyRequests, RetryAfter = TimeSpan.FromSeconds(1) };
        var (handler, _) = Create(inner);

        var response = await SendAsync(handler, HttpMethod.Post);

        inner.Calls.ShouldBe(1);
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Patch_429_is_never_retried()
    {
        var inner = new CountingHandler { Status = HttpStatusCode.TooManyRequests, RetryAfter = TimeSpan.FromSeconds(1) };
        var (handler, _) = Create(inner);

        var response = await SendAsync(handler, HttpMethod.Patch);

        inner.Calls.ShouldBe(1);
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Non_429_passes_through_untouched()
    {
        var inner = new CountingHandler { Status = HttpStatusCode.InternalServerError, RetryAfter = TimeSpan.FromSeconds(1) };
        var (handler, _) = Create(inner);

        var response = await SendAsync(handler, HttpMethod.Get);

        inner.Calls.ShouldBe(1);
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RetryAfter_date_is_honored_within_cap()
    {
        var inner = new CountingHandler
        {
            Status = HttpStatusCode.TooManyRequests,
            RetryAfterDate = DateTimeOffset.UtcNow.AddSeconds(1),
        };
        var (handler, _) = Create(inner);

        var response = await SendAsync(handler, HttpMethod.Get);

        inner.Calls.ShouldBe(2);
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Oversized_RetryAfter_is_capped_at_30_seconds()
    {
        var inner = new CountingHandler
        {
            Status = HttpStatusCode.TooManyRequests,
            RetryAfter = TimeSpan.FromMinutes(5),
        };
        var (handler, _) = Create(inner);

        var response = await SendAsync(handler, HttpMethod.Get);

        inner.Calls.ShouldBe(2);
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
