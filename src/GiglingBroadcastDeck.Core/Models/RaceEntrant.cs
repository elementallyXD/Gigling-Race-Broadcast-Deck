namespace GiglingBroadcastDeck.Core.Models;

/// <summary>
/// Normalized participant row for race results or live position overlays.
/// </summary>
/// <remarks>
/// Public API payloads may identify an entrant by pet id, displayed Gigling name, owner name,
/// wallet address, or only a raw id. All fields are nullable except the display label so the UI
/// can show the best available information without assuming a fixed response shape.
/// </remarks>
public sealed record RaceEntrant
{
    public string DisplayName { get; init; } = "Unknown entrant";
    public string? PetId { get; init; }
    public string? PetRarity { get; init; }
    public string? PetGender { get; init; }
    public decimal? PetElo { get; init; }
    public string? OwnerName { get; init; }
    public string? OwnerAddress { get; init; }
    public bool? OwnerHasNoob { get; init; }
    public int? OwnerNoobId { get; init; }
    public int? OwnerPetCount { get; init; }
    public int? OwnerEnergy { get; init; }
    public int? OwnerMaxEnergy { get; init; }
    public string? OwnerTopPetId { get; init; }
    public string? OwnerTopPetName { get; init; }
    public string? OwnerTopPetRarity { get; init; }
    public string? OwnerTopPetGender { get; init; }
    public decimal? OwnerTopPetElo { get; init; }
    public int? Slot { get; init; }
    public DateTimeOffset? JoinedAt { get; init; }
    public bool? IsJuiced { get; init; }
    public decimal? ProtoSurcharge { get; init; }
    public int? FinishTimeMs { get; init; }
    public int? Place { get; init; }
    public decimal? Position { get; init; }
    public decimal? Progress { get; init; }
}
