namespace GiglingBroadcastDeck.Core.Models;

public sealed record LeaderboardEntry
{
    public int? Rank { get; init; }
    public string Name { get; init; } = "Unknown";
    public string? PetId { get; init; }
    public string? Owner { get; init; }
    public string? Faction { get; init; }
    public string? Rarity { get; init; }
    public decimal? Elo { get; init; }
}
