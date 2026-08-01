using FSH.BlazorShared.Services;
using Microsoft.Extensions.Logging;

namespace FSH.BlazorShared.Auth;

public interface IPermissionsProvider
{
    Task<string[]> GetPermissionsAsync(CancellationToken ct = default);
    Task InvalidateCache();
    Task ResetAsync();
    bool IsHydrated { get; }
}

public sealed class PermissionsProvider(IAuthService authService, ITokenStore tokenStore, ILogger<PermissionsProvider> logger) : IPermissionsProvider
{
    private string[]? _cached;
    private readonly object _lock = new();

    public bool IsHydrated => Volatile.Read(ref _cached) is not null;

    public async Task<string[]> GetPermissionsAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cached is not null)
                return _cached;
        }

        var stored = await tokenStore.GetPermissionsAsync();
        if (stored is not null)
        {
            lock (_lock) _cached = stored;
            return stored;
        }

        try
        {
            var permissions = await authService.GetPermissionsAsync(ct);
            await tokenStore.SetPermissionsAsync(permissions);
            lock (_lock) _cached = permissions;
            return permissions;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch permissions");
            return [];
        }
    }

    public Task InvalidateCache()
    {
        lock (_lock) _cached = null;
        return Task.CompletedTask;
    }

    /// <summary>Drops the in-memory cache AND the persisted copy (login-time reset, React parity).</summary>
    public async Task ResetAsync()
    {
        lock (_lock) _cached = null;
        await tokenStore.ClearPermissionsAsync();
    }
}
