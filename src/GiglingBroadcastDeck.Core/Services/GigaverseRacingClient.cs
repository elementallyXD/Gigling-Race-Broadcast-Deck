using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Options;
using Microsoft.Extensions.Logging;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Read-only HTTP client for public Gigling Racing REST data.
/// </summary>
/// <remarks>
/// The client returns raw JSON so the mapper can preserve source transparency and tolerate
/// response-shape changes. It never calls authenticated or gameplay mutation endpoints.
/// </remarks>
public sealed class GigaverseRacingClient(
    HttpClient httpClient,
    GigaverseOptions options,
    ILogger<GigaverseRacingClient> logger) : IGigaverseRacingClient
{
    private const int ErrorBodyPreviewLimit = 240;
    private const int LeaderboardLimit = 25;

    /// <inheritdoc />
    public Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken) =>
        GetRawAsync($"races?limit={options.RaceLimit}", cancellationToken);

    /// <inheritdoc />
    public Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken) =>
        GetRawAsync($"race/{Uri.EscapeDataString(raceId)}", cancellationToken);

    /// <inheritdoc />
    public Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken) =>
        GetRawAsync($"race-state?raceId={Uri.EscapeDataString(raceId)}", cancellationToken);

    /// <inheritdoc />
    public Task<ApiFetchResult<string>> GetScheduledRacesRawAsync(CancellationToken cancellationToken) =>
        GetRawAsync("scheduled", cancellationToken);

    /// <inheritdoc />
    public Task<ApiFetchResult<string>> GetGlobalStatsRawAsync(CancellationToken cancellationToken) =>
        GetRawAsync("stats", cancellationToken);

    /// <inheritdoc />
    public Task<ApiFetchResult<string>> GetLeaderboardRawAsync(CancellationToken cancellationToken) =>
        GetRawAsync($"leaderboard/elo?limit={LeaderboardLimit}&offset=0", cancellationToken);

    private async Task<ApiFetchResult<string>> GetRawAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fetchedAt = DateTimeOffset.UtcNow;

        try
        {
            using var response = await httpClient.GetAsync(relativePath, cancellationToken).ConfigureAwait(false);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Gigaverse Racing public API returned {StatusCode} for {Path}.",
                    (int)response.StatusCode,
                    relativePath);

                return ApiFetchResult<string>.Failure(
                    $"Gigaverse API returned {(int)response.StatusCode} {response.ReasonPhrase}: {Trim(raw)}",
                    fetchedAt);
            }

            return ApiFetchResult<string>.Success(raw, fetchedAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gigaverse Racing public API request failed for {Path}.", relativePath);
            return ApiFetchResult<string>.Failure(ex.Message, fetchedAt);
        }
    }

    private static string Trim(string value) =>
        value.Length <= ErrorBodyPreviewLimit ? value : value[..ErrorBodyPreviewLimit] + "...";
}
