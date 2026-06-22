using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public sealed class RacePollingService(IGigaverseRacingClient client, IRaceMapper mapper) : IRacePollingService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private RaceSummary? _selectedRace;

    public RaceDataSnapshot Snapshot { get; private set; } = new();

    public async Task<RaceDataSnapshot> RefreshRecentRacesAsync(CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return Snapshot;
        }

        try
        {
            var result = await client.GetRecentRacesRawAsync(cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                return MarkFailure(result.ErrorMessage ?? "Unable to fetch recent races.", result.FetchedAt);
            }

            var races = mapper.MapRecentRaces(result.Value, result.FetchedAt);
            Snapshot = Snapshot with
            {
                Races = races,
                Status = races.Count == 0 ? AppStatus.Empty : AppStatus.Ready,
                Message = races.Count == 0 ? "No recent races returned." : $"Loaded {races.Count} recent races.",
                IsStale = false,
                LastSuccessfulFetchAt = result.FetchedAt,
                LastAttemptedFetchAt = result.FetchedAt
            };

            return Snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RaceDataSnapshot> SelectRaceAsync(RaceSummary race, CancellationToken cancellationToken)
    {
        _selectedRace = race;
        return await RefreshSelectedRaceAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<RaceDataSnapshot> RefreshSelectedRaceAsync(CancellationToken cancellationToken)
    {
        if (_selectedRace is null)
        {
            return Snapshot;
        }

        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return Snapshot;
        }

        try
        {
            var result = await client.GetRaceDetailRawAsync(_selectedRace.RaceId, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                return MarkFailure(result.ErrorMessage ?? "Unable to fetch selected race.", result.FetchedAt);
            }

            var detail = mapper.MapRaceDetail(result.Value, _selectedRace.RaceId, result.FetchedAt);
            Snapshot = Snapshot with
            {
                SelectedRaceDetail = detail,
                Status = AppStatus.Ready,
                Message = $"Loaded race #{detail.RaceId}.",
                IsStale = false,
                LastSuccessfulFetchAt = result.FetchedAt,
                LastAttemptedFetchAt = result.FetchedAt
            };

            return Snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    private RaceDataSnapshot MarkFailure(string message, DateTimeOffset attemptedAt)
    {
        Snapshot = Snapshot with
        {
            Status = Snapshot.LastSuccessfulFetchAt is null ? AppStatus.Error : AppStatus.Stale,
            Message = message,
            IsStale = Snapshot.LastSuccessfulFetchAt is not null,
            LastAttemptedFetchAt = attemptedAt
        };

        return Snapshot;
    }
}
