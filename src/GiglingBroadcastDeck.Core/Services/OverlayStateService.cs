using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public sealed class OverlayStateService : IOverlayStateService
{
    private readonly object _gate = new();
    private OverlayState _state = new();

    public OverlayState GetSnapshot()
    {
        lock (_gate)
        {
            return _state;
        }
    }

    public OverlayState SetMode(OverlayMode mode, RaceSummary? selectedRace, RaceDetail? selectedRaceDetail)
    {
        lock (_gate)
        {
            var headline = CreateHeadline(mode, selectedRace, selectedRaceDetail);
            _state = _state with
            {
                Mode = mode,
                SelectedRace = selectedRace,
                SelectedRaceDetail = selectedRaceDetail,
                Headline = headline,
                TickerItems = CreateTickerItems(selectedRace, selectedRaceDetail),
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return _state;
        }
    }

    public OverlayState Hide()
    {
        lock (_gate)
        {
            _state = _state with { Mode = OverlayMode.Hidden, UpdatedAt = DateTimeOffset.UtcNow };
            return _state;
        }
    }

    private static string CreateHeadline(OverlayMode mode, RaceSummary? selectedRace, RaceDetail? selectedRaceDetail)
    {
        var raceId = selectedRaceDetail?.RaceId ?? selectedRace?.RaceId ?? "unknown";
        var phase = selectedRaceDetail?.Phase ?? selectedRace?.Phase ?? "Unknown";

        return mode switch
        {
            OverlayMode.RaceCard => $"Race #{raceId} - {phase}",
            OverlayMode.ResultCard => $"Result - Race #{raceId}",
            OverlayMode.Ticker => $"Gigling Racing: Race #{raceId} is {phase}",
            _ => ""
        };
    }

    private static IReadOnlyList<string> CreateTickerItems(RaceSummary? selectedRace, RaceDetail? selectedRaceDetail)
    {
        if (selectedRace is null && selectedRaceDetail is null)
        {
            return ["Select a race in Gigling Broadcast Deck"];
        }

        var raceId = selectedRaceDetail?.RaceId ?? selectedRace?.RaceId ?? "unknown";
        var phase = selectedRaceDetail?.Phase ?? selectedRace?.Phase ?? "Unknown";
        var entrants = FormatEntrants(selectedRaceDetail?.EntrantCount ?? selectedRace?.EntrantCount, selectedRaceDetail?.MaxEntrants ?? selectedRace?.MaxEntrants);

        return [$"Race #{raceId}", $"Phase: {phase}", $"Entrants: {entrants}"];
    }

    private static string FormatEntrants(int? current, int? max) =>
        current is null && max is null ? "Unknown" : $"{current?.ToString() ?? "?"}/{max?.ToString() ?? "?"}";
}
