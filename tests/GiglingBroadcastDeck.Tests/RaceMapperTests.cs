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

    [Fact]
    public void MapRecentRaces_ReadsNestedDataRacesWrapper()
    {
        const string rawJson = """
        {
          "data": {
            "races": [
              { "id": "nested-1", "phase": "OPEN" }
            ]
          }
        }
        """;

        var races = _mapper.MapRecentRaces(rawJson, DateTimeOffset.UnixEpoch);

        Assert.Single(races);
        Assert.Equal("nested-1", races[0].RaceId);
    }

    [Fact]
    public void MapRaceDetail_MapsRaceIntelligenceFields()
    {
        const string rawJson = """
        {
          "race": {
            "id": "rich-1",
            "phase": "OPEN",
            "entryFeeWei": "10000000000000000",
            "trackLength": 1200,
            "creator": "0xabc",
            "isPrivate": false,
            "currentNetPrizePool": "30000000000000000",
            "projectedNetPrizePool": "80000000000000000",
            "projectedPayouts": ["50000000000000000", "30000000000000000"],
            "payoutDistribution": [6250, 3750],
            "weather": 2,
            "faction": 8,
            "items": 3,
            "source": "indexer"
          }
        }
        """;

        var detail = _mapper.MapRaceDetail(rawJson, "fallback", DateTimeOffset.UnixEpoch);

        Assert.Equal("rich-1", detail.RaceId);
        Assert.Equal(0.01m, detail.EntryFee);
        Assert.Equal(1200, detail.TrackLength);
        Assert.Equal("0xabc", detail.Creator);
        Assert.False(detail.IsPrivate);
        Assert.Equal(0.03m, detail.CurrentNetPrizePool);
        Assert.Equal(0.08m, detail.ProjectedNetPrizePool);
        Assert.Equal([0.05m, 0.03m], detail.ProjectedPayouts);
        Assert.Equal([6250, 3750], detail.PayoutDistribution);
        Assert.Equal("Rainy", detail.Weather);
        Assert.Equal("Gigus", detail.Faction);
        Assert.Equal("All", detail.ItemsMode);
        Assert.Equal("indexer", detail.Source);
    }

    [Fact]
    public void MapGlobalStats_FlattensSimpleStatsObject()
    {
        const string rawJson = """
        {
          "stats": {
            "totalRaces": 120,
            "activePets": 44,
            "jackpotBalance": "1.5"
          }
        }
        """;

        var stats = _mapper.MapGlobalStats(rawJson);

        Assert.Contains(stats, item => item.Label == "total Races" && item.Value == "120");
        Assert.Contains(stats, item => item.Label == "active Pets" && item.Value == "44");
    }

    [Fact]
    public void MapLeaderboard_ToleratesCommonLeaderboardFields()
    {
        const string rawJson = """
        {
          "data": [
            { "rank": 1, "petName": "Dash", "petId": "77", "elo": "1550.5", "faction": 1, "rarity": "Legendary" }
          ]
        }
        """;

        var entries = _mapper.MapLeaderboard(rawJson);

        Assert.Single(entries);
        Assert.Equal(1, entries[0].Rank);
        Assert.Equal("Dash", entries[0].Name);
        Assert.Equal("77", entries[0].PetId);
        Assert.Equal(1550.5m, entries[0].Elo);
        Assert.Equal("Crusader", entries[0].Faction);
        Assert.Equal("Legendary", entries[0].Rarity);
    }
}
