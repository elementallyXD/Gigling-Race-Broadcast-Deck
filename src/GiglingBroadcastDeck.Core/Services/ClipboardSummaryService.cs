using System.Globalization;
using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Creates concise copy for Discord or stream chat from selected race data.
/// </summary>
public sealed class ClipboardSummaryService : IClipboardSummaryService
{
    /// <inheritdoc />
    public string CreateSummary(RaceSummary? summary, RaceDetail? detail)
    {
        var raceId = detail?.RaceId ?? summary?.RaceId ?? "unknown";
        var phase = detail?.Phase ?? summary?.Phase ?? "Unknown";
        var pool = detail?.Pool ?? summary?.Pool;
        var entryFee = detail?.EntryFee ?? summary?.EntryFee;
        var trackLength = detail?.TrackLength ?? summary?.TrackLength;
        var entrants = FormatEntrants(detail?.EntrantCount ?? summary?.EntrantCount, detail?.MaxEntrants ?? summary?.MaxEntrants);
        var updatedAt = (detail?.LastFetchedAt ?? summary?.LastFetchedAt ?? DateTimeOffset.UtcNow)
            .ToUniversalTime()
            .ToString("HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        var resultSection = FormatResultSection(phase, detail);

        return $"""
        Gigling Racing - Race #{raceId}
        Phase: {phase}
        Entrants: {entrants}
        Pool: {FormatEth(pool)}
        Entry Fee: {FormatEth(entryFee)}
        Track: {FormatTrackLength(trackLength)}
        {resultSection}
        Updated: {updatedAt}
        Source: Gigling Broadcast Deck reads public Gigling Racing API data only.
        """;
    }

    private static string FormatEntrants(int? current, int? max) =>
        current is null && max is null ? "Unknown" : $"{current?.ToString(CultureInfo.InvariantCulture) ?? "?"} / {max?.ToString(CultureInfo.InvariantCulture) ?? "?"}";

    private static string FormatEth(decimal? value) =>
        value is null ? "Unknown" : $"{value.Value:0.####} ETH";

    private static string FormatTrackLength(int? value) =>
        value is null ? "Unknown" : $"{value.Value.ToString(CultureInfo.InvariantCulture)}m";

    private static string FormatResultSection(string phase, RaceDetail? detail)
    {
        if (!string.Equals(phase, "RESOLVED", StringComparison.OrdinalIgnoreCase) || detail is null)
        {
            return "Results: Not available yet";
        }

        var payouts = detail.CurrentPayouts.Count > 0 ? detail.CurrentPayouts : detail.ProjectedPayouts;

        if (detail.ResultEntrants.Count > 0)
        {
            var entrantLines = detail.ResultEntrants
                .Select((entrant, index) => FormatPlaceLine(index, ResolveEntrant(entrant, detail.LivePositions), payouts))
                .ToArray();

            return "Results:" + Environment.NewLine + string.Join(Environment.NewLine, entrantLines);
        }

        if (detail.ResultOrder.Count == 0)
        {
            return "Results: Not available yet";
        }

        var lines = detail.ResultOrder
            .Select((entrant, index) => FormatPlaceLine(index, ResolveEntrant(entrant, detail.LivePositions), payouts))
            .ToArray();

        return "Results:" + Environment.NewLine + string.Join(Environment.NewLine, lines);
    }

    private static string FormatPlaceLine(int index, string entrant, IReadOnlyList<decimal> payouts)
    {
        var place = index + 1;
        var payout = index < payouts.Count ? $" - {FormatEth(payouts[index])}" : "";
        return $"{place}. {entrant}{payout}";
    }

    private static string FormatPlaceLine(int index, RaceEntrant entrant, IReadOnlyList<decimal> payouts)
    {
        var owner = FirstNonEmpty(entrant.OwnerName, entrant.OwnerAddress);
        var ownerText = string.IsNullOrWhiteSpace(owner) ? "" : $" (owner: {owner})";
        return FormatPlaceLine(index, $"{entrant.DisplayName}{ownerText}", payouts);
    }

    private static RaceEntrant ResolveEntrant(RaceEntrant entrant, IReadOnlyList<RaceEntrant> references)
    {
        var reference = references.FirstOrDefault(candidate => EntrantsMatch(entrant, candidate));
        if (reference is null)
        {
            return entrant;
        }

        return entrant with
        {
            DisplayName = ShouldUseReferenceDisplayName(entrant, reference) ? reference.DisplayName : entrant.DisplayName,
            PetId = FirstNonEmpty(entrant.PetId, reference.PetId),
            OwnerName = FirstNonEmpty(entrant.OwnerName, reference.OwnerName),
            OwnerAddress = FirstNonEmpty(entrant.OwnerAddress, reference.OwnerAddress)
        };
    }

    private static RaceEntrant ResolveEntrant(string entrant, IReadOnlyList<RaceEntrant> references)
    {
        var reference = references.FirstOrDefault(candidate =>
            SameNonEmptyText(entrant, candidate.PetId) ||
            SameNonEmptyText(entrant, candidate.DisplayName) ||
            SameNonEmptyText(entrant, candidate.OwnerAddress));

        return reference ?? new RaceEntrant { DisplayName = entrant, PetId = entrant };
    }

    private static bool EntrantsMatch(RaceEntrant entrant, RaceEntrant reference) =>
        SameNonEmptyText(entrant.PetId, reference.PetId) ||
        SameNonEmptyText(entrant.DisplayName, reference.PetId) ||
        SameNonEmptyText(entrant.PetId, reference.DisplayName) ||
        SameNonEmptyText(entrant.DisplayName, reference.DisplayName) ||
        SameNonEmptyText(entrant.OwnerAddress, reference.OwnerAddress);

    private static bool ShouldUseReferenceDisplayName(RaceEntrant entrant, RaceEntrant reference) =>
        !string.IsNullOrWhiteSpace(reference.DisplayName) &&
        (string.IsNullOrWhiteSpace(entrant.DisplayName) ||
            entrant.DisplayName.StartsWith("Entrant ", StringComparison.OrdinalIgnoreCase) ||
            SameNonEmptyText(entrant.DisplayName, entrant.PetId) ||
            SameNonEmptyText(entrant.DisplayName, reference.PetId));

    private static bool SameNonEmptyText(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
