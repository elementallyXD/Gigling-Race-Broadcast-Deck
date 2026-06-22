namespace GiglingBroadcastDeck.Core.Services;

public sealed class DisabledRealtimeRaceFeed : IRealtimeRaceFeed
{
    public bool IsEnabled => false;

    public Task StartAsync(string? raceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
