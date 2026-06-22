namespace GiglingBroadcastDeck.Core.Models;

public sealed record StatLine
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
}
