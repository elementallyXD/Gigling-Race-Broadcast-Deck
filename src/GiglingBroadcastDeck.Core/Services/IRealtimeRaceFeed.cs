namespace GiglingBroadcastDeck.Core.Services;

public interface IRealtimeRaceFeed
{
    bool IsEnabled { get; }

    Task StartAsync(string? raceId, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
