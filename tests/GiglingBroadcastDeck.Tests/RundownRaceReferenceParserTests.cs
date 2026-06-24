using GiglingBroadcastDeck.Core.Services;

namespace GiglingBroadcastDeck.Tests;

public sealed class RundownRaceReferenceParserTests
{
    [Fact]
    public void TryExtractRaceId_ReturnsId_ForPinnedRaceLine()
    {
        var raceId = RundownRaceReferenceParser.TryExtractRaceId("Race #13934 - RESOLVED - Entrants 4/4");

        Assert.Equal("13934", raceId);
    }

    [Fact]
    public void TryExtractRaceId_ReturnsNull_ForFreeFormTickerLine()
    {
        var raceId = RundownRaceReferenceParser.TryExtractRaceId("Next up: final lobby closes in 30 seconds");

        Assert.Null(raceId);
    }

    [Fact]
    public void TryExtractRaceId_TrimsWhitespaceAroundId()
    {
        var raceId = RundownRaceReferenceParser.TryExtractRaceId("  Race # abc-123  - OPEN - Entrants 2/8  ");

        Assert.Equal("abc-123", raceId);
    }
}
