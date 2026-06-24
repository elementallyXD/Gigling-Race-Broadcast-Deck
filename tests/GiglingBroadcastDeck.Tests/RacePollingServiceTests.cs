using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

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
        var service = CreateService(client);

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
        var service = CreateService(client);

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
        var service = CreateService(client);

        var snapshot = await service.RefreshRecentRacesAsync(CancellationToken.None);

        Assert.Equal(AppStatus.Error, snapshot.Status);
        Assert.Contains("Unable to parse recent races response", snapshot.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>temporary outage</html>")]
    public async Task RefreshRecentRacesAsync_MalformedSuccessAfterGoodFetchKeepsLastGoodSnapshot(string malformedBody)
    {
        var client = new ScriptedGigaverseClient(
            recentRaceResults:
            [
                ApiFetchResult<string>.Success("""[{ "id": "race-1", "phase": "OPEN" }]""", DateTimeOffset.UnixEpoch),
                ApiFetchResult<string>.Success(malformedBody, DateTimeOffset.UnixEpoch.AddSeconds(5))
            ]);
        var service = CreateService(client);

        await service.RefreshRecentRacesAsync(CancellationToken.None);
        var stale = await service.RefreshRecentRacesAsync(CancellationToken.None);

        Assert.Equal(AppStatus.Stale, stale.Status);
        Assert.True(stale.IsStale);
        Assert.Single(stale.Races);
        Assert.Equal("race-1", stale.Races[0].RaceId);
        Assert.Contains("Unable to parse recent races response", stale.Message);
    }

    [Fact]
    public async Task RefreshSelectedRaceAsync_MalformedSuccessAfterGoodFetchKeepsLastGoodDetail()
    {
        var client = new ScriptedGigaverseClient(
            detailResults:
            [
                ApiFetchResult<string>.Success("""{ "id": "race-2", "phase": "OPEN" }""", DateTimeOffset.UnixEpoch),
                ApiFetchResult<string>.Success("<html>temporary outage</html>", DateTimeOffset.UnixEpoch.AddSeconds(5))
            ]);
        var service = CreateService(client);

        await service.SelectRaceAsync(new RaceSummary { RaceId = "race-2" }, CancellationToken.None);
        var stale = await service.RefreshSelectedRaceAsync(CancellationToken.None);

        Assert.Equal(AppStatus.Stale, stale.Status);
        Assert.True(stale.IsStale);
        Assert.Equal("race-2", stale.SelectedRaceDetail?.RaceId);
        Assert.Equal("OPEN", stale.SelectedRaceDetail?.Phase);
        Assert.Contains("Unable to parse selected race response", stale.Message);
    }

    [Fact]
    public async Task RefreshSelectedRaceAsync_EnrichesOwnerNamesFromPublicNoobSummaries()
    {
        var client = new ScriptedGigaverseClient(
            detailResult: ApiFetchResult<string>.Success(
                """
                {
                  "raceId": 13951,
                  "phase": 3,
                  "entries": [
                    { "petId": 24015, "ownerAddress": "0xowner-a" },
                    { "petId": 4560, "ownerAddress": "0xowner-b" }
                  ],
                  "finalRanking": [24015, 4560]
                }
                """,
                DateTimeOffset.UnixEpoch),
            noobSummaryResults: new Dictionary<string, ApiFetchResult<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["0xowner-a"] = ApiFetchResult<string>.Success(
                    """
                    {
                      "success": true,
                      "summary": {
                        "username": "vision83",
                        "hasNoob": true,
                        "noobId": 77823,
                        "energyValue": 420,
                        "maxEnergy": 420,
                        "petCount": 34,
                        "topRacingGigling": {
                          "petId": 24015,
                          "name": "#24015",
                          "rarity": 4,
                          "gender": "Male",
                          "elo": 1677
                        }
                      }
                    }
                    """,
                    DateTimeOffset.UnixEpoch),
                ["0xowner-b"] = ApiFetchResult<string>.Success("""{ "success": true, "summary": { "username": "oneindigheid", "petCount": 34 } }""", DateTimeOffset.UnixEpoch)
            });
        var service = CreateService(client);

        var snapshot = await service.SelectRaceAsync(new RaceSummary { RaceId = "13951" }, CancellationToken.None);

        Assert.Equal("vision83", snapshot.SelectedRaceDetail?.ResultEntrants[0].OwnerName);
        Assert.Equal(77823, snapshot.SelectedRaceDetail?.ResultEntrants[0].OwnerNoobId);
        Assert.Equal(34, snapshot.SelectedRaceDetail?.ResultEntrants[0].OwnerPetCount);
        Assert.Equal(420, snapshot.SelectedRaceDetail?.ResultEntrants[0].OwnerEnergy);
        Assert.Equal(420, snapshot.SelectedRaceDetail?.ResultEntrants[0].OwnerMaxEnergy);
        Assert.Equal("#24015", snapshot.SelectedRaceDetail?.ResultEntrants[0].OwnerTopPetName);
        Assert.Equal("Legendary", snapshot.SelectedRaceDetail?.ResultEntrants[0].PetRarity);
        Assert.Equal("Male", snapshot.SelectedRaceDetail?.ResultEntrants[0].PetGender);
        Assert.Equal(1677, snapshot.SelectedRaceDetail?.ResultEntrants[0].PetElo);
        Assert.Equal("oneindigheid", snapshot.SelectedRaceDetail?.ResultEntrants[1].OwnerName);
        Assert.Equal(34, snapshot.SelectedRaceDetail?.ResultEntrants[1].OwnerPetCount);
    }

    private sealed class ScriptedGigaverseClient(
        ApiFetchResult<string>[]? recentRaceResults = null,
        ApiFetchResult<string>[]? detailResults = null,
        ApiFetchResult<string>? detailResult = null,
        ApiFetchResult<string>? stateResult = null,
        IReadOnlyDictionary<string, ApiFetchResult<string>>? noobSummaryResults = null) : IGigaverseRacingClient
    {
        private readonly Queue<ApiFetchResult<string>> _recentRaceResults = new(recentRaceResults ?? []);
        private readonly Queue<ApiFetchResult<string>> _detailResults = new(detailResults ?? []);
        private readonly ApiFetchResult<string> _detailResult =
            detailResult ?? ApiFetchResult<string>.Success("""{ "id": "race", "phase": "OPEN" }""", DateTimeOffset.UnixEpoch);
        private readonly ApiFetchResult<string> _stateResult =
            stateResult ?? ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch);

        public Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_recentRaceResults.Dequeue());

        public Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(_detailResults.Count > 0 ? _detailResults.Dequeue() : _detailResult);

        public Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken) =>
            Task.FromResult(_stateResult);

        public Task<ApiFetchResult<string>> GetScheduledRacesRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));

        public Task<ApiFetchResult<string>> GetGlobalStatsRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));

        public Task<ApiFetchResult<string>> GetLeaderboardRawAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));

        public Task<ApiFetchResult<string>> GetNoobSummaryRawAsync(string walletAddress, CancellationToken cancellationToken) =>
            Task.FromResult(noobSummaryResults is not null && noobSummaryResults.TryGetValue(walletAddress, out var result)
                ? result
                : ApiFetchResult<string>.Failure("not used", DateTimeOffset.UnixEpoch));
    }

    private static RacePollingService CreateService(IGigaverseRacingClient client) =>
        new(client, new RaceMapper(), NullLogger<RacePollingService>.Instance);
}
