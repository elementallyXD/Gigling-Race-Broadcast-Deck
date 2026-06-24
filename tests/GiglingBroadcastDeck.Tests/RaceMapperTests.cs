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
    public void MapRaceDetail_MapsResultOwnersAndLivePositions()
    {
        const string rawJson = """
        {
          "race": {
            "id": "race-live",
            "phase": "RESOLVING",
            "finalRanking": [
              { "petId": "101", "petName": "Dash", "ownerName": "alice" },
              { "petId": "102", "petName": "Bolt", "ownerAddress": "0xabc" }
            ],
            "currentPositions": [
              { "petId": "slow", "petName": "Slowpoke", "ownerName": "carol", "distance": 200 },
              { "petId": "fast", "petName": "Rocket", "ownerName": "bob", "distance": 800 }
            ]
          }
        }
        """;

        var detail = _mapper.MapRaceDetail(rawJson, "fallback", DateTimeOffset.UnixEpoch);

        Assert.Equal("Dash", detail.ResultEntrants[0].DisplayName);
        Assert.Equal("alice", detail.ResultEntrants[0].OwnerName);
        Assert.Equal("Bolt", detail.ResultEntrants[1].DisplayName);
        Assert.Equal("0xabc", detail.ResultEntrants[1].OwnerAddress);
        Assert.Equal("Rocket", detail.LivePositions[0].DisplayName);
        Assert.Equal("bob", detail.LivePositions[0].OwnerName);
        Assert.Equal(800, detail.LivePositions[0].Position);
    }

    [Fact]
    public void MapRaceDetail_EnrichesResultIdsFromEntrantNicknames()
    {
        const string rawJson = """
        {
          "data": {
            "race": {
              "id": "race-owner-names",
              "phase": "RESOLVED",
              "finalRanking": ["101", "102"]
            },
            "entrants": [
              { "petId": "101", "petName": "Dash", "owner": { "nickname": "alice" } },
              { "giglingId": "102", "giglingName": "Bolt", "player": { "username": "bob" } }
            ]
          }
        }
        """;

        var detail = _mapper.MapRaceDetail(rawJson, "fallback", DateTimeOffset.UnixEpoch);

        Assert.Equal("Dash", detail.ResultEntrants[0].DisplayName);
        Assert.Equal("alice", detail.ResultEntrants[0].OwnerName);
        Assert.Equal("Bolt", detail.ResultEntrants[1].DisplayName);
        Assert.Equal("bob", detail.ResultEntrants[1].OwnerName);
    }

    [Fact]
    public void MapRaceDetail_EnrichesResultIdsFromEntriesAndPetOwners()
    {
        const string rawJson = """
        {
          "raceId": 13951,
          "phase": 3,
          "entries": [
            { "petId": 24015, "ownerAddress": "0xowner-a", "slot": 1, "juiced": true },
            { "petId": 4560, "ownerAddress": "0xowner-b" }
          ],
          "petOwners": {
            "14792": "0xowner-c"
          },
          "finalRanking": [24015, 4560, 14792],
          "finishTimes": [56819, 57854, 58383]
        }
        """;

        var detail = _mapper.MapRaceDetail(rawJson, "fallback", DateTimeOffset.UnixEpoch);

        Assert.Equal("24015", detail.ResultEntrants[0].DisplayName);
        Assert.Equal("0xowner-a", detail.ResultEntrants[0].OwnerAddress);
        Assert.Equal(1, detail.ResultEntrants[0].Slot);
        Assert.True(detail.ResultEntrants[0].IsJuiced);
        Assert.Equal(56819, detail.ResultEntrants[0].FinishTimeMs);
        Assert.Equal("4560", detail.ResultEntrants[1].DisplayName);
        Assert.Equal("0xowner-b", detail.ResultEntrants[1].OwnerAddress);
        Assert.Equal(57854, detail.ResultEntrants[1].FinishTimeMs);
        Assert.Equal("14792", detail.ResultEntrants[2].DisplayName);
        Assert.Equal("0xowner-c", detail.ResultEntrants[2].OwnerAddress);
        Assert.Equal(58383, detail.ResultEntrants[2].FinishTimeMs);
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
            "source": { "type": "manual" }
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
        Assert.Equal("Manual", detail.RaceType);
        Assert.Equal("manual", detail.Source);
    }

    [Fact]
    public void MapRaceDetail_MapsRaceTempAsWeather()
    {
        const string rawJson = """
        {
          "success": true,
          "raceId": 13952,
          "phase": 2,
          "raceTemp": "cold",
          "source": { "type": "manual" }
        }
        """;

        var detail = _mapper.MapRaceDetail(rawJson, "fallback", DateTimeOffset.UnixEpoch);

        Assert.Equal("Cold", detail.Weather);
        Assert.Equal("Manual", detail.RaceType);
        Assert.Equal("manual", detail.Source);
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
