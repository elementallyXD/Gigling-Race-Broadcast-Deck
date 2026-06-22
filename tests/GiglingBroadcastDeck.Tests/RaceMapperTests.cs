using GiglingBroadcastDeck.Core.Mapping;

namespace GiglingBroadcastDeck.Tests;

public sealed class RaceMapperTests
{
    private readonly RaceMapper _mapper = new();

    [Fact]
    public void MapRecentRaces_ToleratesMissingAndWrongTypes()
    {
        const string rawJson = """
        {
          "races": [
            {
              "race_id": "abc",
              "status": 1,
              "currentEntries": "4",
              "fieldSize": "8",
              "projectedGrossPool": "2500000000000000000"
            },
            {
              "status": null,
              "currentEntries": {}
            }
          ]
        }
        """;

        var races = _mapper.MapRecentRaces(rawJson, DateTimeOffset.UnixEpoch);

        Assert.Single(races);
        Assert.Equal("abc", races[0].RaceId);
        Assert.Equal("OPEN", races[0].Phase);
        Assert.Equal(4, races[0].EntrantCount);
        Assert.Equal(8, races[0].MaxEntrants);
        Assert.Equal(2.5m, races[0].Pool);
        Assert.False(string.IsNullOrWhiteSpace(races[0].RawJson));
    }

    [Fact]
    public void MapRaceDetail_PreservesRawJsonAndResultOrder()
    {
        const string rawJson = """
        {
          "data": {
            "race": {
              "id": "race-7",
              "phase": "resolved",
              "finalRanking": [
                { "petId": "gigling-1" },
                { "petId": "gigling-2" }
              ]
            }
          }
        }
        """;

        var detail = _mapper.MapRaceDetail(rawJson, "fallback", DateTimeOffset.UnixEpoch);

        Assert.Equal("race-7", detail.RaceId);
        Assert.Equal("RESOLVED", detail.Phase);
        Assert.Equal(["gigling-1", "gigling-2"], detail.ResultOrder);
        Assert.Equal(rawJson, detail.RawJson);
    }
}
