using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Mapping;

public interface IRaceMapper
{
    IReadOnlyList<RaceSummary> MapRecentRaces(string rawJson, DateTimeOffset fetchedAt);

    RaceDetail MapRaceDetail(string rawJson, string raceId, DateTimeOffset fetchedAt);
}
