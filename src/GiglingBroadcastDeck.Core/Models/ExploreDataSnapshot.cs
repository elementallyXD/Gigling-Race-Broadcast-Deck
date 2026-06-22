namespace GiglingBroadcastDeck.Core.Models;

public sealed record ExploreDataSnapshot
{
    public IReadOnlyList<RaceSummary> ScheduledRaces { get; init; } = [];
    public IReadOnlyList<StatLine> GlobalStats { get; init; } = [];
    public IReadOnlyList<LeaderboardEntry> Leaderboard { get; init; } = [];
    public AppStatus Status { get; init; } = AppStatus.Idle;
    public string Message { get; init; } = "Explore data has not loaded yet.";
    public DateTimeOffset? LastFetchedAt { get; init; }
}
