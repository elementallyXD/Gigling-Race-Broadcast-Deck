namespace GiglingBroadcastDeck.Core.Options;

public sealed class GigaverseOptions
{
    public string BaseUrl { get; set; } = "https://gigaverse.io/api/racing";
    public int RaceLimit { get; set; } = 50;
    public int TimeoutSeconds { get; set; } = 10;
}
