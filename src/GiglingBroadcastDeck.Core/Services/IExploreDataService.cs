using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Loads read-only ecosystem context for the Explore tab.
/// </summary>
public interface IExploreDataService
{
    /// <summary>
    /// Refreshes scheduled races, global stats, and leaderboard data with partial-failure tolerance.
    /// </summary>
    Task<ExploreDataSnapshot> RefreshAsync(CancellationToken cancellationToken);
}
