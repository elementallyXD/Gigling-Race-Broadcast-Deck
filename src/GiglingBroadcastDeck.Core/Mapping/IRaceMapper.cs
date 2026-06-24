using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Mapping;

/// <summary>
/// Maps changing public Gigling Racing JSON payloads into stable app-owned models.
/// </summary>
public interface IRaceMapper
{
    /// <summary>
    /// Maps a public recent-races payload into race summaries.
    /// </summary>
    IReadOnlyList<RaceSummary> MapRecentRaces(string rawJson, DateTimeOffset fetchedAt);

    /// <summary>
    /// Maps a public race detail or race-state payload into a detail model.
    /// </summary>
    RaceDetail MapRaceDetail(string rawJson, string raceId, DateTimeOffset fetchedAt);

    /// <summary>
    /// Maps a public scheduled-races payload into race summaries.
    /// </summary>
    IReadOnlyList<RaceSummary> MapScheduledRaces(string rawJson, DateTimeOffset fetchedAt);

    /// <summary>
    /// Maps public global stats into simple label/value rows.
    /// </summary>
    IReadOnlyList<StatLine> MapGlobalStats(string rawJson);

    /// <summary>
    /// Maps public ELO leaderboard rows.
    /// </summary>
    IReadOnlyList<LeaderboardEntry> MapLeaderboard(string rawJson);
}
