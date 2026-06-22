using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public interface IGigaverseRacingClient
{
    Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken);

    Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken);

    Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken);
}
