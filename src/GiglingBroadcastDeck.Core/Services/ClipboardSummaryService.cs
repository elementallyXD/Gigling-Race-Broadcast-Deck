using System.Globalization;
using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public sealed class ClipboardSummaryService : IClipboardSummaryService
{
    public string CreateSummary(RaceSummary? summary, RaceDetail? detail)
    {
        var raceId = detail?.RaceId ?? summary?.RaceId ?? "unknown";
        var phase = detail?.Phase ?? summary?.Phase ?? "Unknown";
        var pool = detail?.Pool ?? summary?.Pool;
        var entrants = FormatEntrants(detail?.EntrantCount ?? summary?.EntrantCount, detail?.MaxEntrants ?? summary?.MaxEntrants);
        var updatedAt = (detail?.LastFetchedAt ?? summary?.LastFetchedAt ?? DateTimeOffset.UtcNow)
            .ToUniversalTime()
            .ToString("HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        return $"""
        Gigling Racing - Race #{raceId}
        Phase: {phase}
        Entrants: {entrants}
        Pool: {FormatEth(pool)}
        Updated: {updatedAt}
        Source: Gigling Broadcast Deck reads public Gigling Racing API data only.
        """;
    }

    private static string FormatEntrants(int? current, int? max) =>
        current is null && max is null ? "Unknown" : $"{current?.ToString(CultureInfo.InvariantCulture) ?? "?"} / {max?.ToString(CultureInfo.InvariantCulture) ?? "?"}";

    private static string FormatEth(decimal? value) =>
        value is null ? "Unknown" : $"{value.Value:0.####} ETH";
}
