namespace GiglingBroadcastDeck.Core.Options;

public sealed class PollingOptions
{
    public int RecentRacesSeconds { get; set; } = 15;
    public int SelectedRaceSeconds { get; set; } = 5;
    public int StaleAfterSeconds { get; set; } = 45;
}
