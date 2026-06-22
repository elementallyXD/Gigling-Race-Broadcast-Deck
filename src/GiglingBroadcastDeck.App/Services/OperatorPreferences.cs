using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.App.Services;

public sealed record OperatorPreferences
{
    public OverlayPreset OverlayPreset { get; init; } = OverlayPreset.Broadcast;
    public OverlayPosition OverlayPosition { get; init; } = OverlayPosition.LowerLeft;
    public IReadOnlyList<string> RundownItems { get; init; } = [];
}
