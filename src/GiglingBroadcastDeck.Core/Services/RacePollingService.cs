using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Models;
using System.Text.Json;

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

            IReadOnlyList<RaceSummary> races;
            try
            {
                races = mapper.MapRecentRaces(result.Value, result.FetchedAt);
            }
            catch (JsonException ex)
            {
                return MarkFailure($"Unable to parse recent races response: {ex.Message}", result.FetchedAt);
            }

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
                return await TryLoadRaceStateFallbackAsync(
                    result.ErrorMessage ?? "Unable to fetch selected race.",
                    result.FetchedAt,
                    cancellationToken).ConfigureAwait(false);
            }

            RaceDetail detail;
            try
            {
                detail = mapper.MapRaceDetail(result.Value, _selectedRace.RaceId, result.FetchedAt);
            }
            catch (JsonException ex)
            {
                return MarkFailure($"Unable to parse selected race response: {ex.Message}", result.FetchedAt);
            }

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

    private async Task<RaceDataSnapshot> TryLoadRaceStateFallbackAsync(
        string detailErrorMessage,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken)
    {
        if (_selectedRace is null)
        {
            return MarkFailure(detailErrorMessage, attemptedAt);
        }

        var fallback = await client.GetRaceStateRawAsync(_selectedRace.RaceId, cancellationToken).ConfigureAwait(false);
        if (!fallback.IsSuccess || fallback.Value is null)
        {
            return MarkFailure($"{detailErrorMessage} Fallback race-state also failed: {fallback.ErrorMessage}", fallback.FetchedAt);
        }

        RaceDetail detail;
        try
        {
            detail = mapper.MapRaceDetail(fallback.Value, _selectedRace.RaceId, fallback.FetchedAt);
        }
        catch (JsonException ex)
        {
            return MarkFailure($"Unable to parse race-state fallback response: {ex.Message}", fallback.FetchedAt);
        }

        Snapshot = Snapshot with
        {
            SelectedRaceDetail = detail,
            Status = AppStatus.Unavailable,
            Message = $"Race detail endpoint unavailable; showing race-state fallback. {detailErrorMessage}",
            IsStale = false,
            LastAttemptedFetchAt = fallback.FetchedAt
        };

        return Snapshot;
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
