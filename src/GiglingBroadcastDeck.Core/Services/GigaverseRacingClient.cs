using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Options;

namespace GiglingBroadcastDeck.Core.Services;

public sealed class GigaverseRacingClient(HttpClient httpClient, GigaverseOptions options) : IGigaverseRacingClient
{
    public Task<ApiFetchResult<string>> GetRecentRacesRawAsync(CancellationToken cancellationToken) =>
        GetRawAsync($"races?limit={options.RaceLimit}", cancellationToken);

    public Task<ApiFetchResult<string>> GetRaceDetailRawAsync(string raceId, CancellationToken cancellationToken) =>
        GetRawAsync($"race/{Uri.EscapeDataString(raceId)}", cancellationToken);

    public Task<ApiFetchResult<string>> GetRaceStateRawAsync(string raceId, CancellationToken cancellationToken) =>
        GetRawAsync($"race-state?raceId={Uri.EscapeDataString(raceId)}", cancellationToken);

    private async Task<ApiFetchResult<string>> GetRawAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fetchedAt = DateTimeOffset.UtcNow;

        try
        {
            using var response = await httpClient.GetAsync(relativePath, cancellationToken).ConfigureAwait(false);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
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
            return ApiFetchResult<string>.Failure(ex.Message, fetchedAt);
        }
    }

    private static string Trim(string value) =>
        value.Length <= 240 ? value : value[..240] + "...";
}
