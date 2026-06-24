namespace GiglingBroadcastDeck.Core.Options;

/// <summary>
/// Feature flag for future public realtime race feeds.
/// </summary>
/// <remarks>
/// The MVP leaves realtime disabled and uses REST polling. Enabling realtime must remain read-only.
/// </remarks>
public sealed class RealtimeOptions
{
    public bool Enabled { get; set; }
}
