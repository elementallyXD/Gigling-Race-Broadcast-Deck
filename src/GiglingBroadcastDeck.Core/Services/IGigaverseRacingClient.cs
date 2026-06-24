using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Fetches public Gigling Racing REST endpoints as raw JSON strings.
/// </summary>
/// <remarks>
/// This interface is intentionally read-only. Implementations must not call authenticated,
/// gameplay, wallet, transaction, or POST endpoints.
/// </remarks>
public interface IGigaverseRacingClient
{
    /// <summary>
    /// Gets the public recent-races payload.
    /// </summary>
    Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the public detail payload for one race.
    /// </summary>
    Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the public diagnostic race-state payload used only as a fallback display source.
    /// </summary>
    Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the public scheduled-races payload.
    /// </summary>
    Task<ApiFetchResult<string>> GetScheduledRacesRawAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets public global racing stats.
    /// </summary>
    Task<ApiFetchResult<string>> GetGlobalStatsRawAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets public ELO leaderboard data.
    /// </summary>
    Task<ApiFetchResult<string>> GetLeaderboardRawAsync(CancellationToken cancellationToken);
}
