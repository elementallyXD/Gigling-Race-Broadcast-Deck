namespace GiglingBroadcastDeck.Core.Models;

public sealed record RaceSummary
{
    public string RaceId { get; init; } = "";
    public string Phase { get; init; } = "Unknown";
    public decimal? Pool { get; init; }
    public decimal? EntryFee { get; init; }
    public int? EntrantCount { get; init; }
    public int? MaxEntrants { get; init; }
    public DateTimeOffset LastFetchedAt { get; init; }
    public string RawJson { get; init; } = "";
}
