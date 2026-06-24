using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Owns the in-memory overlay state returned by the local browser-source API.
/// </summary>
public interface IOverlayStateService
{
    /// <summary>
    /// Returns the latest immutable overlay state snapshot.
    /// </summary>
    OverlayState GetSnapshot();

    /// <summary>
    /// Changes overlay mode and binds it to the current race context.
    /// </summary>
    OverlayState SetMode(OverlayMode mode, RaceSummary? selectedRace, RaceDetail? selectedRaceDetail);

    /// <summary>
    /// Changes visual preset and placement without changing selected race data.
    /// </summary>
    OverlayState SetPreset(OverlayPreset preset, OverlayPosition position);

    /// <summary>
    /// Replaces pinned rundown lines. Empty input intentionally clears pinned ticker fallback text.
    /// </summary>
    OverlayState SetRundown(IReadOnlyList<string> rundownItems);

    /// <summary>
    /// Hides the overlay while preserving selected race context.
    /// </summary>
    OverlayState Hide();
}
