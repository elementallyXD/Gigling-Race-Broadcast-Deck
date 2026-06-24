namespace GiglingBroadcastDeck.App.Services;

/// <summary>
/// Controls the local Kestrel server that serves OBS overlay assets and state.
/// </summary>
public interface ILocalOverlayServer
{
    /// <summary>
    /// Gets whether the local server has started successfully.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the browser-source overlay URL.
    /// </summary>
    string OverlayUrl { get; }

    /// <summary>
    /// Gets the local health endpoint URL.
    /// </summary>
    string HealthUrl { get; }

    /// <summary>
    /// Starts the local overlay server.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops and disposes the local overlay server.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
