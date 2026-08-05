using FSH.Hybrid.Services;
using Shouldly;
using Xunit;

namespace FSH.Hybrid.Tests;

public sealed class OfflineQueueServiceTests : IAsyncDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fsh-offline-tests-{Guid.NewGuid():N}.db3");
    private readonly List<OfflineQueueService> _queues = [];

    public async ValueTask DisposeAsync()
    {
        foreach (var queue in _queues)
        {
            await queue.DisposeAsync();
        }

        File.Delete(_dbPath);
    }

    private OfflineQueueService CreateQueue()
    {
        var queue = new OfflineQueueService(_dbPath);
        _queues.Add(queue);
        return queue;
    }

    [Fact]
    public async Task EnqueueAndSnapshot_PreservesFifoOrderAndPayload()
    {
        var queue = CreateQueue();
        await queue.EnqueueAsync(new QueuedRequest(null, "POST", "https://api.example.com/v1/x", "{\"a\":1}", "application/json", DateTimeOffset.UtcNow, 0));
        await queue.EnqueueAsync(new QueuedRequest(null, "DELETE", "https://api.example.com/v1/y", null, string.Empty, DateTimeOffset.UtcNow, 0));

        var snapshot = await queue.SnapshotAsync();

        snapshot.Count.ShouldBe(2);
        snapshot[0].Method.ShouldBe("POST");
        snapshot[0].Body.ShouldBe("{\"a\":1}");
        snapshot[0].ContentType.ShouldBe("application/json");
        snapshot[1].Method.ShouldBe("DELETE");
        snapshot[1].Id.ShouldNotBeNull();
    }

    [Fact]
    public async Task PersistsAcrossReopen()
    {
        var queue = CreateQueue();
        await queue.EnqueueAsync(new QueuedRequest(null, "PUT", "https://api.example.com/v1/z", "body", "text/plain", DateTimeOffset.UtcNow, 0));

        var reopened = CreateQueue();
        var snapshot = await reopened.SnapshotAsync();

        snapshot.Single().Url.ShouldBe("https://api.example.com/v1/z");
        snapshot.Single().Body.ShouldBe("body");
        (await reopened.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task RemoveAsync_RemovesItem()
    {
        var queue = CreateQueue();
        await queue.EnqueueAsync(new QueuedRequest(null, "POST", "https://api.example.com/v1/x", null, string.Empty, DateTimeOffset.UtcNow, 0));
        var id = (await queue.SnapshotAsync()).Single().Id!.Value;

        await queue.RemoveAsync(id);

        (await queue.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task MarkFailedAsync_IncrementsRetryCount()
    {
        var queue = CreateQueue();
        await queue.EnqueueAsync(new QueuedRequest(null, "POST", "https://api.example.com/v1/x", null, string.Empty, DateTimeOffset.UtcNow, 0));
        var id = (await queue.SnapshotAsync()).Single().Id!.Value;

        await queue.MarkFailedAsync(id);
        await queue.MarkFailedAsync(id);

        (await queue.SnapshotAsync()).Single().RetryCount.ShouldBe(2);
    }

    [Fact]
    public async Task MarkFailedAsync_DropsItemAfterMaxRetries()
    {
        var queue = CreateQueue();
        await queue.EnqueueAsync(new QueuedRequest(null, "POST", "https://api.example.com/v1/x", null, string.Empty, DateTimeOffset.UtcNow, 0));
        var id = (await queue.SnapshotAsync()).Single().Id!.Value;

        for (var i = 0; i < OfflineQueueService.MaxRetries; i++)
        {
            await queue.MarkFailedAsync(id);
        }

        (await queue.CountAsync()).ShouldBe(0);
    }
}
