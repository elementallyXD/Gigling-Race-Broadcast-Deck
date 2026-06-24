using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Services;

namespace GiglingBroadcastDeck.Tests;

public sealed class OverlayStateServiceTests
{
    [Fact]
    public void SetMode_StoresSelectedRaceAndCreatesTickerItems()
    {
        var service = CreateService();
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
        var service = CreateService();
        service.SetMode(OverlayMode.RaceCard, new RaceSummary { RaceId = "99" }, null);

        var state = service.Hide();

        Assert.Equal(OverlayMode.Hidden, state.Mode);
        Assert.Equal("99", state.SelectedRace?.RaceId);
    }

    [Fact]
    public void SetPresetAndRundown_UpdateOverlaySnapshot()
    {
        var service = CreateService();

        service.SetPreset(OverlayPreset.DataDesk, OverlayPosition.TopRight);
        var state = service.SetRundown(["Race #1 is open", "Race #2 resolved"]);

        Assert.Equal(OverlayPreset.DataDesk, state.Preset);
        Assert.Equal(OverlayPosition.TopRight, state.Position);
        Assert.Equal(["Race #1 is open", "Race #2 resolved"], state.RundownItems);
        Assert.Equal(["Race #1 is open", "Race #2 resolved"], state.TickerItems);
    }

    [Fact]
    public void SetRundown_WithEmptyList_ClearsPinnedTickerFallback()
    {
        var service = CreateService();

        service.SetRundown(["Race #1 is open", "Race #2 resolved"]);
        var state = service.SetRundown([]);

        Assert.Empty(state.RundownItems);
        Assert.Empty(state.TickerItems);
    }

    private static OverlayStateService CreateService() =>
        new(new RacePhaseExplainer());
}
