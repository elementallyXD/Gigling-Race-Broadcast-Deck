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
            recentRaceResults:
            [
                ApiFetchResult<string>.Success("""[{ "id": "race-1", "phase": "OPEN" }]""", DateTimeOffset.UnixEpoch),
                ApiFetchResult<string>.Failure("temporary failure", DateTimeOffset.UnixEpoch.AddSeconds(5))
            ]);
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

    [Fact]
    public async Task RefreshSelectedRaceAsync_UsesRaceStateFallbackWhenDetailEndpointFails()
    {
        var client = new ScriptedGigaverseClient(
            detailResult: ApiFetchResult<string>.Failure("detail unavailable", DateTimeOffset.UnixEpoch),
            stateResult: ApiFetchResult<string>.Success("""{ "id": "race-2", "status": "RESOLVING" }""", DateTimeOffset.UnixEpoch.AddSeconds(1)));
        var service = new RacePollingService(client, new RaceMapper());

        var snapshot = await service.SelectRaceAsync(new RaceSummary { RaceId = "race-2" }, CancellationToken.None);

        Assert.Equal(AppStatus.Unavailable, snapshot.Status);
        Assert.Equal("race-2", snapshot.SelectedRaceDetail?.RaceId);
        Assert.Equal("RESOLVING", snapshot.SelectedRaceDetail?.Phase);
        Assert.Contains("race-state fallback", snapshot.Message);
    }

    [Fact]
    public async Task RefreshRecentRacesAsync_ReturnsErrorWhenJsonCannotBeParsed()
    {
        var client = new ScriptedGigaverseClient(
            recentRaceResults:
            [
                ApiFetchResult<string>.Success("{ not valid json", DateTimeOffset.UnixEpoch)
            ]);
        var service = new RacePollingService(client, new RaceMapper());

        var snapshot = await service.RefreshRecentRacesAsync(CancellationToken.None);

        Assert.Equal(AppStatus.Error, snapshot.Status);
        Assert.Contains("Unable to parse recent races response", snapshot.Message);
    }

    private sealed class ScriptedGigaverseClient(
        ApiFetchResult<string>[]? recentRaceResults = null,
        ApiFetchResult<string>? detailResult = null,
        ApiFetchResult<string>? stateResult = null) : IGigaverseRacingClient
    {
        private readonly Queue<ApiFetchResult<string>> _recentRaceResults = new(recentRaceResults ?? []);
        private readonly ApiFetchResult<string> _detailResult =
            detailResult ?? ApiFetchResult<string>.Success("""{ "id": "race", "phase": "OPEN" }""", DateTimeOffset.UnixEpoch);
        private readonly ApiFetchResult<string> _stateResult =
            stateResult ?? ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch);

        public Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_recentRaceResults.Dequeue());

        public Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(_detailResult);

        public Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(_stateResult);
    }
}
