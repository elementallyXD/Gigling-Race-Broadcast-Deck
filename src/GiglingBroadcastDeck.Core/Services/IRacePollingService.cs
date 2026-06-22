using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public interface IRacePollingService
{
    RaceDataSnapshot Snapshot { get; }

    Task<RaceDataSnapshot> RefreshRecentRacesAsync(CancellationToken cancellationToken);

    Task<RaceDataSnapshot> SelectRaceAsync(RaceSummary race, CancellationToken cancellationToken);

    Task<RaceDataSnapshot> RefreshSelectedRaceAsync(CancellationToken cancellationToken);
}
