namespace GiglingBroadcastDeck.Core.Models;

public sealed record RaceDataSnapshot
{
    public IReadOnlyList<RaceSummary> Races { get; init; } = [];
    public RaceDetail? SelectedRaceDetail { get; init; }
    public AppStatus Status { get; init; } = AppStatus.Idle;
    public string Message { get; init; } = "Ready";
    public bool IsStale { get; init; }
    public DateTimeOffset? LastSuccessfulFetchAt { get; init; }
    public DateTimeOffset? LastAttemptedFetchAt { get; init; }
}
