using System.Net;
using System.Text;
using FSH.Hybrid.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Hybrid.Tests;

public sealed class OfflineDelegatingHandlerTests : IAsyncDisposable
{
    private readonly List<OfflineQueueService> _queues = [];

    public async ValueTask DisposeAsync()
    {
        foreach (var queue in _queues)
        {
            await queue.DisposeAsync();
        }
    }

    private (HttpClient Client, OfflineQueueService Queue, IConnectivityService Connectivity) CreateSut(HttpMessageHandler inner)
    {
        var connectivity = Substitute.For<IConnectivityService>();
        connectivity.IsOnline.Returns(true);
        var queue = new OfflineQueueService(Path.Combine(Path.GetTempPath(), $"fsh-offline-handler-{Guid.NewGuid():N}.db3"));
        _queues.Add(queue);
        var handler = new OfflineDelegatingHandler(queue, connectivity, NullLogger<OfflineDelegatingHandler>.Instance)
        {
            InnerHandler = inner,
        };
        return (new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") }, queue, connectivity);
    }

    [Fact]
    public async Task MutatingRequestWhileOffline_QueuesAndThrowsOfflineException()
    {
        var (client, queue, connectivity) = CreateSut(new StubHandler(() => throw new InvalidOperationException("inner must not be called")));
        connectivity.IsOnline.Returns(false);
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/tickets")
        {
            Content = new StringContent("{\"title\":\"hi\"}", Encoding.UTF8, "application/json"),
        };

        await Should.ThrowAsync<OfflineException>(() => client.SendAsync(request));

        var snapshot = await queue.SnapshotAsync();
        var item = snapshot.Single();
        item.Method.ShouldBe("POST");
        item.Url.ShouldBe("https://api.example.com/v1/tickets");
        item.Body.ShouldBe("{\"title\":\"hi\"}");
        item.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public async Task GetWhileOffline_PassesThrough()
    {
        var (client, queue, connectivity) = CreateSut(new StubHandler(() => new HttpResponseMessage(HttpStatusCode.OK)));
        connectivity.IsOnline.Returns(false);

        var response = await client.GetAsync("/v1/tickets");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await queue.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task MutatingRequestWhileOnline_SendsNormally()
    {
        var (client, queue, _) = CreateSut(new StubHandler(() => new HttpResponseMessage(HttpStatusCode.Created)));
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/tickets")
        {
            Content = new StringContent("{\"title\":\"hi\"}", Encoding.UTF8, "application/json"),
        };

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await queue.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task MutatingRequestDroppingMidFlight_QueuesAndThrowsOfflineException()
    {
        var (client, queue, connectivity) = CreateSut(new StubHandler(() => throw new HttpRequestException("connection reset")));
        connectivity.IsOnline.Returns(false);

        await Should.ThrowAsync<OfflineException>(() => client.PostAsync("/v1/tickets", new StringContent("{}")));

        (await queue.CountAsync()).ShouldBe(1);
    }

    private sealed class StubHandler(Func<HttpResponseMessage?> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder() ?? throw new HttpRequestException("network down"));
    }
}
