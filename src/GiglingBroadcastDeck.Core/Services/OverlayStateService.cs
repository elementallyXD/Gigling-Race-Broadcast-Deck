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
                SourceNote = CreateSourceNote(selectedRace, selectedRaceDetail),
                LifecycleText = RacePhaseDescriptionService.Describe(selectedRaceDetail?.Phase ?? selectedRace?.Phase),
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return _state;
        }
    }

    public OverlayState SetPreset(OverlayPreset preset, OverlayPosition position)
    {
        lock (_gate)
        {
            _state = _state with
            {
                Preset = preset,
                Position = position,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return _state;
        }
    }

    public OverlayState SetRundown(IReadOnlyList<string> rundownItems)
    {
        lock (_gate)
        {
            _state = _state with
            {
                RundownItems = rundownItems.Where(item => !string.IsNullOrWhiteSpace(item)).Take(5).ToArray(),
                TickerItems = rundownItems.Count > 0 ? rundownItems.Take(5).ToArray() : _state.TickerItems,
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

        var pool = selectedRaceDetail?.Pool ?? selectedRace?.Pool;
        return pool is null
            ? [$"Race #{raceId}", $"Phase: {phase}", $"Entrants: {entrants}"]
            : [$"Race #{raceId}", $"Phase: {phase}", $"Entrants: {entrants}", $"Pool: {pool:0.####} ETH"];
    }

    private static string CreateSourceNote(RaceSummary? selectedRace, RaceDetail? selectedRaceDetail)
    {
        var source = selectedRaceDetail?.Source ?? selectedRace?.Source;
        return string.IsNullOrWhiteSpace(source)
            ? "Source: public Gigling Racing API"
            : $"Source: {source}";
    }

    private static string FormatEntrants(int? current, int? max) =>
        current is null && max is null ? "Unknown" : $"{current?.ToString() ?? "?"}/{max?.ToString() ?? "?"}";
}
