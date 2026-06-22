using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Mapping;

public interface IRaceMapper
{
    IReadOnlyList<RaceSummary> MapRecentRaces(string rawJson, DateTimeOffset fetchedAt);

    RaceDetail MapRaceDetail(string rawJson, string raceId, DateTimeOffset fetchedAt);

    IReadOnlyList<RaceSummary> MapScheduledRaces(string rawJson, DateTimeOffset fetchedAt);

    IReadOnlyList<StatLine> MapGlobalStats(string rawJson);

    IReadOnlyList<LeaderboardEntry> MapLeaderboard(string rawJson);
}
