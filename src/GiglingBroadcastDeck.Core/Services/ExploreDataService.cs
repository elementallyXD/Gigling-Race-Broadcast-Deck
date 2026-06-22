using System.Text.Json;
using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public sealed class ExploreDataService(IGigaverseRacingClient client, IRaceMapper mapper) : IExploreDataService
{
    public async Task<ExploreDataSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        var fetchedAt = DateTimeOffset.UtcNow;
        var messages = new List<string>();

        var scheduled = await FetchScheduledAsync(messages, cancellationToken).ConfigureAwait(false);
        var stats = await FetchStatsAsync(messages, cancellationToken).ConfigureAwait(false);
        var leaderboard = await FetchLeaderboardAsync(messages, cancellationToken).ConfigureAwait(false);

        var loadedAny = scheduled.Count > 0 || stats.Count > 0 || leaderboard.Count > 0;
        return new ExploreDataSnapshot
        {
            ScheduledRaces = scheduled,
            GlobalStats = stats,
            Leaderboard = leaderboard,
            Status = loadedAny ? AppStatus.Ready : AppStatus.Unavailable,
            Message = messages.Count == 0 ? "Explore data loaded." : string.Join(" ", messages),
            LastFetchedAt = fetchedAt
        };
    }

    private async Task<IReadOnlyList<RaceSummary>> FetchScheduledAsync(List<string> messages, CancellationToken cancellationToken)
    {
        var result = await client.GetScheduledRacesRawAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            messages.Add($"Scheduled unavailable: {result.ErrorMessage}");
            return [];
        }

        try
        {
            return mapper.MapScheduledRaces(result.Value, result.FetchedAt);
        }
        catch (JsonException ex)
        {
            messages.Add($"Scheduled parse failed: {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<StatLine>> FetchStatsAsync(List<string> messages, CancellationToken cancellationToken)
    {
        var result = await client.GetGlobalStatsRawAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            messages.Add($"Stats unavailable: {result.ErrorMessage}");
            return [];
        }

        try
        {
            return mapper.MapGlobalStats(result.Value);
        }
        catch (JsonException ex)
        {
            messages.Add($"Stats parse failed: {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<LeaderboardEntry>> FetchLeaderboardAsync(List<string> messages, CancellationToken cancellationToken)
    {
        var result = await client.GetLeaderboardRawAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            messages.Add($"Leaderboard unavailable: {result.ErrorMessage}");
            return [];
        }

        try
        {
            return mapper.MapLeaderboard(result.Value);
        }
        catch (JsonException ex)
        {
            messages.Add($"Leaderboard parse failed: {ex.Message}");
            return [];
        }
    }
}
