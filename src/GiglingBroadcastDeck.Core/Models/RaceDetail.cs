namespace GiglingBroadcastDeck.Core.Models;

/// <summary>
/// Detailed normalized race data used by the operator panel and overlay.
/// </summary>
/// <remarks>
/// Most fields are nullable because public API shapes can change or omit optional race metadata.
/// Raw JSON is preserved for transparency and hackathon judge verification.
/// </remarks>
public sealed record RaceDetail
{
    public string RaceId { get; init; } = "";
    public string Phase { get; init; } = "Unknown";
    public IReadOnlyList<string> ResultOrder { get; init; } = [];
    public decimal? Pool { get; init; }
    public decimal? EntryFee { get; init; }
    public int? EntrantCount { get; init; }
    public int? MaxEntrants { get; init; }
    public int? TrackLength { get; init; }
    public string? Creator { get; init; }
    public bool? IsPrivate { get; init; }
    public DateTimeOffset? RaceStart { get; init; }
    public DateTimeOffset? RaceFinish { get; init; }
    public decimal? CurrentNetPrizePool { get; init; }
    public decimal? ProjectedNetPrizePool { get; init; }
    public IReadOnlyList<decimal> CurrentPayouts { get; init; } = [];
    public IReadOnlyList<decimal> ProjectedPayouts { get; init; } = [];
    public IReadOnlyList<int> PayoutDistribution { get; init; } = [];
    public string? Weather { get; init; }
    public string? Faction { get; init; }
    public string? ItemsMode { get; init; }
    public string? Source { get; init; }
    public DateTimeOffset LastFetchedAt { get; init; }
    public string RawJson { get; init; } = "";
}
