namespace GiglingBroadcastDeck.App.Services;

public interface ILocalOverlayServer
{
    bool IsRunning { get; }
    string OverlayUrl { get; }
    string HealthUrl { get; }

    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
