using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Services;

namespace GiglingBroadcastDeck.Tests;

public sealed class OverlayStateServiceTests
{
    [Fact]
    public void SetMode_StoresSelectedRaceAndCreatesTickerItems()
    {
        var service = new OverlayStateService();
        var race = new RaceSummary
        {
            RaceId = "42",
            Phase = "OPEN",
            EntrantCount = 3,
            MaxEntrants = 8
        };

        var state = service.SetMode(OverlayMode.Ticker, race, null);

        Assert.Equal(OverlayMode.Ticker, state.Mode);
        Assert.Equal("42", state.SelectedRace?.RaceId);
        Assert.Contains("Race #42", state.TickerItems);
        Assert.Contains("Phase: OPEN", state.TickerItems);
    }

    [Fact]
    public void Hide_OnlyChangesModeAndKeepsSnapshotReadable()
    {
        var service = new OverlayStateService();
        service.SetMode(OverlayMode.RaceCard, new RaceSummary { RaceId = "99" }, null);

        var state = service.Hide();

        Assert.Equal(OverlayMode.Hidden, state.Mode);
        Assert.Equal("99", state.SelectedRace?.RaceId);
    }
}
