using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GiglingBroadcastDeck.Tests;

public sealed class ExploreDataServiceTests
{
    [Fact]
    public async Task RefreshAsync_ReturnsPartialDataWhenOneEndpointFails()
    {
        var client = new ExploreClient
        {
            Scheduled = ApiFetchResult<string>.Success("""[{ "id": "scheduled-1", "phase": "OPEN" }]""", DateTimeOffset.UnixEpoch),
            Stats = ApiFetchResult<string>.Failure("stats outage", DateTimeOffset.UnixEpoch),
            Leaderboard = ApiFetchResult<string>.Success("""[{ "petName": "Dash", "elo": 1400 }]""", DateTimeOffset.UnixEpoch)
        };
        var service = new ExploreDataService(client, new RaceMapper(), NullLogger<ExploreDataService>.Instance);

        var snapshot = await service.RefreshAsync(CancellationToken.None);

        Assert.Equal(AppStatus.Ready, snapshot.Status);
        Assert.Single(snapshot.ScheduledRaces);
        Assert.Empty(snapshot.GlobalStats);
        Assert.Single(snapshot.Leaderboard);
        Assert.Contains("Stats unavailable", snapshot.Message);
    }

    private sealed class ExploreClient : IGigaverseRacingClient
    {
        public ApiFetchResult<string> Scheduled { get; init; } = ApiFetchResult<string>.Failure("not set", DateTimeOffset.UnixEpoch);
        public ApiFetchResult<string> Stats { get; init; } = ApiFetchResult<string>.Failure("not set", DateTimeOffset.UnixEpoch);
        public ApiFetchResult<string> Leaderboard { get; init; } = ApiFetchResult<string>.Failure("not set", DateTimeOffset.UnixEpoch);

        public Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));

        public Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));

        public Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));

        public Task<ApiFetchResult<string>> GetScheduledRacesRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Scheduled);

        public Task<ApiFetchResult<string>> GetGlobalStatsRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Stats);

        public Task<ApiFetchResult<string>> GetLeaderboardRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Leaderboard);
    }
}
