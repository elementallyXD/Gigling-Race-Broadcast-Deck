using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Services;

namespace GiglingBroadcastDeck.Tests;

public sealed class ClipboardSummaryServiceTests
{
    [Fact]
    public void CreateSummary_FormatsUsefulRaceSummary()
    {
        var service = new ClipboardSummaryService();
        var detail = new RaceDetail
        {
            RaceId = "alpha",
            Phase = "RESOLVED",
            EntrantCount = 8,
            MaxEntrants = 8,
            Pool = 3.25m,
            EntryFee = 0.1m,
            TrackLength = 1200,
            LastFetchedAt = DateTimeOffset.Parse("2026-06-22T12:00:00Z")
        };

        var summary = service.CreateSummary(null, detail);

        Assert.Contains("Race #alpha", summary);
        Assert.Contains("Phase: RESOLVED", summary);
        Assert.Contains("Entrants: 8 / 8", summary);
        Assert.Contains("Pool: 3.25 ETH", summary);
        Assert.Contains("Entry Fee: 0.1 ETH", summary);
        Assert.Contains("Track: 1200m", summary);
        Assert.Contains("public Gigling Racing API data only", summary);
    }

    [Fact]
    public void CreateSummary_IncludesPlacesAndPayouts_ForResolvedRace()
    {
        var service = new ClipboardSummaryService();
        var detail = new RaceDetail
        {
            RaceId = "final",
            Phase = "RESOLVED",
            ResultOrder = ["pet-alpha", "pet-beta", "pet-gamma"],
            ResultEntrants =
            [
                new RaceEntrant { DisplayName = "Dash", OwnerName = "alice" },
                new RaceEntrant { DisplayName = "Bolt", OwnerAddress = "0xabc" },
                new RaceEntrant { DisplayName = "Zip" }
            ],
            CurrentPayouts = [1.5m, 0.75m],
            LastFetchedAt = DateTimeOffset.Parse("2026-06-22T12:00:00Z")
        };

        var summary = service.CreateSummary(null, detail);

        Assert.Contains("Results:", summary);
        Assert.Contains("1. Dash (owner: alice) - 1.5 ETH", summary);
        Assert.Contains("2. Bolt (owner: 0xabc) - 0.75 ETH", summary);
        Assert.Contains("3. Zip", summary);
    }

    [Fact]
    public void CreateSummary_ResolvesResultOrderOwnersFromLivePositions()
    {
        var service = new ClipboardSummaryService();
        var detail = new RaceDetail
        {
            RaceId = "final",
            Phase = "RESOLVED",
            ResultOrder = ["101", "102"],
            LivePositions =
            [
                new RaceEntrant { DisplayName = "Dash", PetId = "101", OwnerName = "alice" },
                new RaceEntrant { DisplayName = "Bolt", PetId = "102", OwnerName = "bob" }
            ],
            LastFetchedAt = DateTimeOffset.Parse("2026-06-22T12:00:00Z")
        };

        var summary = service.CreateSummary(null, detail);

        Assert.Contains("1. Dash (owner: alice)", summary);
        Assert.Contains("2. Bolt (owner: bob)", summary);
    }
}
