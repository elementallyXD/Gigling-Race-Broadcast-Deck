namespace GiglingBroadcastDeck.Core.Options;

/// <summary>
/// Polling intervals and stale-data thresholds for public race refresh loops.
/// </summary>
public sealed class PollingOptions
{
    public const int DefaultRecentRacesSeconds = 15;
    public const int DefaultSelectedRaceSeconds = 5;
    public const int DefaultStaleAfterSeconds = 45;

    public int RecentRacesSeconds { get; set; } = DefaultRecentRacesSeconds;
    public int SelectedRaceSeconds { get; set; } = DefaultSelectedRaceSeconds;
    public int StaleAfterSeconds { get; set; } = DefaultStaleAfterSeconds;
}
