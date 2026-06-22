namespace GiglingBroadcastDeck.Core.Models;

public sealed record OverlayState
{
    public OverlayMode Mode { get; init; } = OverlayMode.Hidden;
    public OverlayPreset Preset { get; init; } = OverlayPreset.Broadcast;
    public OverlayPosition Position { get; init; } = OverlayPosition.LowerLeft;
    public RaceSummary? SelectedRace { get; init; }
    public RaceDetail? SelectedRaceDetail { get; init; }
    public string Headline { get; init; } = "";
    public string SourceNote { get; init; } = "";
    public string LifecycleText { get; init; } = "";
    public IReadOnlyList<string> TickerItems { get; init; } = [];
    public IReadOnlyList<string> RundownItems { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
