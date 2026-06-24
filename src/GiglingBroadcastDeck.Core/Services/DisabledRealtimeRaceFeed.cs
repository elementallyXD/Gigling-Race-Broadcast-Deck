namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// No-op realtime feed used while the MVP relies on public REST polling.
/// </summary>
public sealed class DisabledRealtimeRaceFeed : IRealtimeRaceFeed
{
    /// <inheritdoc />
    public bool IsEnabled => false;

    /// <inheritdoc />
    public Task StartAsync(string? raceId, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
