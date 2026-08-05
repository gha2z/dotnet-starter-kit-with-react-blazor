using System.Text;
using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

public interface IOfflineQueueProcessor
{
    Task ProcessQueueAsync(CancellationToken ct = default);
}

/// <summary>
/// Replays the offline queue FIFO when connectivity is restored. Replays go through the
/// "FSH.Api" pipeline so auth headers are re-applied with the current token. A request
/// that fails the retry cap is dropped by <see cref="OfflineQueueService"/>.
/// </summary>
public sealed class OfflineQueueProcessor(
    IOfflineQueueService queue,
    IConnectivityService connectivity,
    IHttpClientFactory httpFactory,
    ILogger<OfflineQueueProcessor> logger) : IOfflineQueueProcessor
{
    public Task ProcessQueueAsync(CancellationToken ct = default)
        => ProcessQueueCoreAsync(ct);

    private async Task ProcessQueueCoreAsync(CancellationToken ct)
    {
        if (!connectivity.IsOnline)
        {
            return;
        }

        var pending = await queue.SnapshotAsync(ct);
        foreach (var item in pending)
        {
            if (!connectivity.IsOnline)
            {
                // Went offline mid-replay; the next connectivity event restarts processing.
                return;
            }

            using var client = httpFactory.CreateClient("FSH.Api");
            using var request = new HttpRequestMessage(new HttpMethod(item.Method), item.Url);
            if (item.Body is not null)
            {
                request.Content = string.IsNullOrEmpty(item.ContentType)
                    ? new StringContent(item.Body, Encoding.UTF8)
                    : new StringContent(item.Body, Encoding.UTF8, item.ContentType);
            }

            try
            {
                var response = await client.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    await queue.RemoveAsync(item.Id!.Value, ct);
                }
                else
                {
                    await queue.MarkFailedAsync(item.Id!.Value, ct);
                }
            }
            catch (OfflineException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Offline replay failed for {Url}", item.Url);
                await queue.MarkFailedAsync(item.Id!.Value, ct);
            }
        }
    }
}
