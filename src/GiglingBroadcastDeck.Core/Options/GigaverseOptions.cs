namespace GiglingBroadcastDeck.Core.Options;

/// <summary>
/// Runtime settings for read-only public Gigling Racing REST access.
/// </summary>
public sealed class GigaverseOptions
{
    /// <summary>
    /// Default public racing API base URL used when appsettings does not override it.
    /// </summary>
    public const string DefaultBaseUrl = "https://gigaverse.io/api/racing";

    /// <summary>
    /// Number of recent races to request for the operator list.
    /// </summary>
    public const int DefaultRaceLimit = 50;

    /// <summary>
    /// HTTP timeout in seconds for public API calls.
    /// </summary>
    public const int DefaultTimeoutSeconds = 10;

    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public int RaceLimit { get; set; } = DefaultRaceLimit;
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
}
