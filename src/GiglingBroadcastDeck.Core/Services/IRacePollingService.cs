using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Coordinates public REST polling and preserves the last safe race snapshot.
/// </summary>
public interface IRacePollingService
{
    /// <summary>
    /// Gets the most recent snapshot known to the app.
    /// </summary>
    RaceDataSnapshot Snapshot { get; }

    /// <summary>
    /// Refreshes the recent race list unless another polling request is already running.
    /// </summary>
    Task<RaceDataSnapshot> RefreshRecentRacesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Selects a race and immediately fetches its detail payload.
    /// </summary>
    Task<RaceDataSnapshot> SelectRaceAsync(RaceSummary race, CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes the selected race detail, keeping prior data on transient failures.
    /// </summary>
    Task<RaceDataSnapshot> RefreshSelectedRaceAsync(CancellationToken cancellationToken);
}
