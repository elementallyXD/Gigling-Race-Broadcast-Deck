namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Optional realtime race feed abstraction.
/// </summary>
/// <remarks>
/// The MVP keeps this disabled and relies on public REST polling. Future realtime work must
/// resync from REST after reconnects and remain read-only.
/// </remarks>
public interface IRealtimeRaceFeed
{
    /// <summary>
    /// Gets whether realtime updates are enabled for this runtime.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Starts listening to public realtime updates for the supplied race id, if enabled.
    /// </summary>
    Task StartAsync(string? raceId, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the realtime listener, if one is running.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
