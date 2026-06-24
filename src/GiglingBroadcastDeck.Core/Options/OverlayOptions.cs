namespace GiglingBroadcastDeck.Core.Options;

/// <summary>
/// Local overlay server and browser-source polling settings.
/// </summary>
public sealed class OverlayOptions
{
    /// <summary>
    /// Default local host name used by the embedded Kestrel overlay server.
    /// </summary>
    public const string DefaultHost = "localhost";

    /// <summary>
    /// Default operator-friendly local overlay port.
    /// </summary>
    public const int DefaultPort = 5050;

    /// <summary>
    /// Default overlay browser polling interval in milliseconds.
    /// </summary>
    public const int DefaultPollMs = 1000;

    public string Host { get; set; } = DefaultHost;
    public int Port { get; set; } = DefaultPort;
    public int PollMs { get; set; } = DefaultPollMs;

    /// <summary>
    /// Local base address used for the Minimal API server.
    /// </summary>
    public string BaseAddress => $"http://{Host}:{Port}";

    /// <summary>
    /// Browser Source URL for OBS.
    /// </summary>
    public string OverlayUrl => $"{BaseAddress}/overlay";
}
