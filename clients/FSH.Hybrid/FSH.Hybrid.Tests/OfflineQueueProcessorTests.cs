using System.Net;
using FSH.Hybrid.Services;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Hybrid.Tests;

public sealed class OfflineQueueProcessorTests : IAsyncDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fsh-offline-proc-{Guid.NewGuid():N}.db3");
    private readonly List<OfflineQueueService> _queues = [];

    public async ValueTask DisposeAsync()
    {
        foreach (var queue in _queues)
        {
            await queue.DisposeAsync();
        }

        File.Delete(_dbPath);
    }

    private (OfflineQueueProcessor Processor, OfflineQueueService Queue, IConnectivityService Connectivity) CreateSut(HttpMessageHandler inner)
    {
        var connectivity = Substitute.For<IConnectivityService>();
        connectivity.IsOnline.Returns(true);
        var queue = new OfflineQueueService(_dbPath);
        _queues.Add(queue);
        var processor = new OfflineQueueProcessor(
            queue,
            connectivity,
            new FakeHttpClientFactory(inner),
            NullLogger<OfflineQueueProcessor>.Instance);
        return (processor, queue, connectivity);
    }

    private static async Task EnqueueItemAsync(OfflineQueueService queue, string method = "POST")
    {
        await queue.EnqueueAsync(new QueuedRequest(null, method, "https://api.example.com/v1/tickets", "{\"title\":\"hi\"}", "application/json", DateTimeOffset.UtcNow, 0));
    }

    [Fact]
    public async Task ReplaysQueuedItemsFifo_AndRemovesOnSuccess()
    {
        var requests = new List<HttpRequestMessage>();
        var (processor, queue, _) = CreateSut(new StubHandler(() =>
        {
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }, requests));
        await EnqueueItemAsync(queue);
        await queue.EnqueueAsync(new QueuedRequest(null, "PUT", "https://api.example.com/v1/tickets/1", null, string.Empty, DateTimeOffset.UtcNow, 0));

        await processor.ProcessQueueAsync();

        (await queue.CountAsync()).ShouldBe(0);
        requests.Select(r => r.Method).ShouldBe([HttpMethod.Post, HttpMethod.Put]);
        requests.First().Content.ShouldNotBeNull();
    }

    [Fact]
    public async Task FailedReplay_IncrementsRetryCount()
    {
        var (processor, queue, _) = CreateSut(new StubHandler(() => new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        await EnqueueItemAsync(queue);

        await processor.ProcessQueueAsync();

        (await queue.CountAsync()).ShouldBe(1);
        (await queue.SnapshotAsync()).Single().RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task DoesNothingWhileOffline()
    {
        var (processor, queue, connectivity) = CreateSut(new StubHandler(() => throw new InvalidOperationException("inner must not be called")));
        connectivity.IsOnline.Returns(false);
        await EnqueueItemAsync(queue);

        await processor.ProcessQueueAsync();

        (await queue.CountAsync()).ShouldBe(1);
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler inner) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(inner);
    }

    private sealed class StubHandler(
        Func<HttpResponseMessage> responder,
        List<HttpRequestMessage>? captured = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            captured?.Add(request);
            return Task.FromResult(responder());
        }
    }
}
