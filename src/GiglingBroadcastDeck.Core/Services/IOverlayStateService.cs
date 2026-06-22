using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public interface IOverlayStateService
{
    OverlayState GetSnapshot();

    OverlayState SetMode(OverlayMode mode, RaceSummary? selectedRace, RaceDetail? selectedRaceDetail);

    OverlayState SetPreset(OverlayPreset preset, OverlayPosition position);

    OverlayState SetRundown(IReadOnlyList<string> rundownItems);

    OverlayState Hide();
}
