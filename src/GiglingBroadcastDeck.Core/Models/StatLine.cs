namespace GiglingBroadcastDeck.Core.Models;

/// <summary>
/// Simple label/value pair used for public global stats.
/// </summary>
public sealed record StatLine
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
}
