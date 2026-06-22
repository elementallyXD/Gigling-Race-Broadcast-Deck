namespace GiglingBroadcastDeck.Core.Options;

public sealed class OverlayOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5050;
    public int PollMs { get; set; } = 1000;

    public string BaseAddress => $"http://{Host}:{Port}";
    public string OverlayUrl => $"{BaseAddress}/overlay";
}
