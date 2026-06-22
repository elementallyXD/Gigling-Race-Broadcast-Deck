namespace GiglingBroadcastDeck.Core.Models;

public sealed record OverlayState
{
    public OverlayMode Mode { get; init; } = OverlayMode.Hidden;
    public RaceSummary? SelectedRace { get; init; }
    public RaceDetail? SelectedRaceDetail { get; init; }
    public string Headline { get; init; } = "";
    public IReadOnlyList<string> TickerItems { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
