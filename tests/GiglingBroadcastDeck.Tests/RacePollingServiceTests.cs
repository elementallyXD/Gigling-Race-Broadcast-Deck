using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Services;

namespace GiglingBroadcastDeck.Tests;

public sealed class RacePollingServiceTests
{
    [Fact]
    public async Task RefreshRecentRacesAsync_KeepsLastGoodDataAndMarksStaleAfterFailure()
    {
        var client = new ScriptedGigaverseClient(
            ApiFetchResult<string>.Success("""[{ "id": "race-1", "phase": "OPEN" }]""", DateTimeOffset.UnixEpoch),
            ApiFetchResult<string>.Failure("temporary failure", DateTimeOffset.UnixEpoch.AddSeconds(5)));
        var service = new RacePollingService(client, new RaceMapper());

        var first = await service.RefreshRecentRacesAsync(CancellationToken.None);
        var second = await service.RefreshRecentRacesAsync(CancellationToken.None);

        Assert.Equal(AppStatus.Ready, first.Status);
        Assert.Single(first.Races);
        Assert.Equal(AppStatus.Stale, second.Status);
        Assert.True(second.IsStale);
        Assert.Single(second.Races);
        Assert.Equal("temporary failure", second.Message);
    }

    private sealed class ScriptedGigaverseClient(params ApiFetchResult<string>[] recentRaceResults) : IGigaverseRacingClient
    {
        private readonly Queue<ApiFetchResult<string>> _recentRaceResults = new(recentRaceResults);

        public Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_recentRaceResults.Dequeue());

        public Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Success($$"""{ "id": "{{raceId}}", "phase": "OPEN" }""", DateTimeOffset.UnixEpoch));

        public Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));
    }
}
