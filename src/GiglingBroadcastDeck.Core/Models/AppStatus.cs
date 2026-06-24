namespace GiglingBroadcastDeck.Core.Models;

/// <summary>
/// High-level user-facing state for race and Explore data views.
/// </summary>
public enum AppStatus
{
    Idle,
    Loading,
    Ready,
    Empty,
    Stale,
    Error,
    Unavailable
}
