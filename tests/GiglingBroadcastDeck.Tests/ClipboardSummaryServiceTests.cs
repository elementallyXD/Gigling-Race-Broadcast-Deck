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
            LastFetchedAt = DateTimeOffset.Parse("2026-06-22T12:00:00Z")
        };

        var summary = service.CreateSummary(null, detail);

        Assert.Contains("Race #alpha", summary);
        Assert.Contains("Phase: RESOLVED", summary);
        Assert.Contains("Entrants: 8 / 8", summary);
        Assert.Contains("Pool: 3.25 ETH", summary);
        Assert.Contains("public Gigling Racing API data only", summary);
    }
}
