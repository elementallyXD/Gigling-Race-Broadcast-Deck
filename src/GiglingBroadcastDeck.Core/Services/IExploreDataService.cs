using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public interface IExploreDataService
{
    Task<ExploreDataSnapshot> RefreshAsync(CancellationToken cancellationToken);
}
