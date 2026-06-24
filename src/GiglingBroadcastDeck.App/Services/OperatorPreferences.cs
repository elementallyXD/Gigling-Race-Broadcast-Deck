using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.App.Services;

/// <summary>
/// Local operator preferences persisted outside the project tree.
/// </summary>
/// <remarks>
/// Preferences intentionally contain no secrets, tokens, private keys, or cached race data.
/// </remarks>
public sealed record OperatorPreferences
{
    public OverlayPreset OverlayPreset { get; init; } = OverlayPreset.Broadcast;
    public OverlayPosition OverlayPosition { get; init; } = OverlayPosition.LowerLeft;
    public IReadOnlyList<string> RundownItems { get; init; } = [];
}
