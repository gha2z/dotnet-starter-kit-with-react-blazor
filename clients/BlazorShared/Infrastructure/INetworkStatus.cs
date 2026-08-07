namespace FSH.BlazorShared.Infrastructure;

/// <summary>
/// Browser connectivity state (React parity: dashboard offline banner).
/// </summary>
public interface INetworkStatus
{
    /// <summary>Current connectivity state.</summary>
    bool IsOnline { get; }

    /// <summary>Raised whenever the browser reports an online/offline transition.</summary>
    event Action? StatusChanged;

    /// <summary>Starts listening to the browser's online/offline events (idempotent).</summary>
    Task InitializeAsync();
}
