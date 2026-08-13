using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

public interface IConnectivityService
{
    bool IsOnline { get; }
    event EventHandler<bool>? ConnectivityChanged;
}

/// <summary>
/// Wraps MAUI <see cref="Connectivity"/> behind an interface so the offline queue stack
/// is unit-testable without a device.
/// </summary>
public sealed class ConnectivityService : IConnectivityService
{
    public ConnectivityService()
    {
        IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        Connectivity.Current.ConnectivityChanged += (_, e) =>
        {
            IsOnline = e.NetworkAccess == NetworkAccess.Internet;
            ConnectivityChanged?.Invoke(this, IsOnline);
        };
    }

    public bool IsOnline { get; private set; }

    public event EventHandler<bool>? ConnectivityChanged;
}
