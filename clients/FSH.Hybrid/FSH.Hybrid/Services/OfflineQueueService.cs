using SQLite;

namespace FSH.Hybrid.Services;

public interface IOfflineQueueService
{
    Task<int> CountAsync(CancellationToken ct = default);
    Task EnqueueAsync(QueuedRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QueuedRequest>> SnapshotAsync(CancellationToken ct = default);
    Task RemoveAsync(long id, CancellationToken ct = default);
    Task MarkFailedAsync(long id, CancellationToken ct = default);
}

public sealed record QueuedRequest(
    long? Id,
    string Method,
    string Url,
    string? Body,
    string ContentType,
    DateTimeOffset CreatedAtUtc,
    int RetryCount);

/// <summary>
/// SQLite-backed FIFO queue of failed mutating HTTP requests. Items are replayed when
/// connectivity returns and dropped after <see cref="MaxRetries"/> consecutive failures.
/// </summary>
public sealed class OfflineQueueService : IOfflineQueueService, IAsyncDisposable
{
    public const int MaxRetries = 3;

    private const string FileName = "offline-queue.db3";

    private readonly SQLiteAsyncConnection _db;
    private readonly Lazy<Task> _initialized;

    public OfflineQueueService(string? databasePath = null)
    {
        var path = databasePath ?? Path.Combine(FileSystem.AppDataDirectory, FileName);
        _db = new SQLiteAsyncConnection(path);
        _initialized = new Lazy<Task>(() => _db.CreateTableAsync<QueuedRequestRow>());
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await _initialized.Value;
        return await _db.Table<QueuedRequestRow>().CountAsync();
    }

    public async Task EnqueueAsync(QueuedRequest request, CancellationToken ct = default)
    {
        await _initialized.Value;
        await _db.InsertAsync(new QueuedRequestRow
        {
            Method = request.Method,
            Url = request.Url,
            Body = request.Body,
            ContentType = request.ContentType,
            CreatedAtUtc = request.CreatedAtUtc.ToString("O"),
            RetryCount = request.RetryCount,
        });
    }

    public async Task<IReadOnlyList<QueuedRequest>> SnapshotAsync(CancellationToken ct = default)
    {
        await _initialized.Value;
        var rows = await _db.Table<QueuedRequestRow>().OrderBy(r => r.Id).ToListAsync();
        return rows
            .Select(r => new QueuedRequest(
                r.Id,
                r.Method,
                r.Url,
                r.Body,
                r.ContentType,
                DateTimeOffset.Parse(r.CreatedAtUtc, System.Globalization.CultureInfo.InvariantCulture),
                r.RetryCount))
            .ToArray();
    }

    public async Task RemoveAsync(long id, CancellationToken ct = default)
    {
        await _initialized.Value;
        await _db.DeleteAsync<QueuedRequestRow>(id);
    }

    public async Task MarkFailedAsync(long id, CancellationToken ct = default)
    {
        await _initialized.Value;
        var row = await _db.Table<QueuedRequestRow>().Where(r => r.Id == id).FirstOrDefaultAsync();
        if (row is null)
        {
            return;
        }

        if (row.RetryCount + 1 >= MaxRetries)
        {
            await _db.DeleteAsync<QueuedRequestRow>(id);
            return;
        }

        row.RetryCount += 1;
        await _db.UpdateAsync(row);
    }

    public async ValueTask DisposeAsync() => await _db.CloseAsync();
}

[Table("queued_requests")]
public sealed class QueuedRequestRow
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    public string Method { get; set; } = default!;

    public string Url { get; set; } = default!;

    public string? Body { get; set; }

    public string ContentType { get; set; } = default!;

    public string CreatedAtUtc { get; set; } = default!;

    public int RetryCount { get; set; }
}
