using System.Text.Json;
using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Mapping;

public sealed class RaceMapper : IRaceMapper
{
    public IReadOnlyList<RaceSummary> MapRecentRaces(string rawJson, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(rawJson);

        return document.RootElement
            .AsLikelyArray()
            .Select(item => MapSummary(item, fetchedAt))
            .Where(race => !string.IsNullOrWhiteSpace(race.RaceId))
            .ToArray();
    }

    public RaceDetail MapRaceDetail(string rawJson, string raceId, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var race = root.FindFirst("race", "data.race", "data") ?? root;

        var summary = MapSummary(race, fetchedAt);
        var resultOrder = ExtractResultOrder(race);

        return new RaceDetail
        {
            RaceId = FirstNonEmpty(summary.RaceId, raceId),
            Phase = summary.Phase,
            Pool = summary.Pool,
            EntrantCount = summary.EntrantCount,
            MaxEntrants = summary.MaxEntrants,
            ResultOrder = resultOrder,
            LastFetchedAt = fetchedAt,
            RawJson = rawJson
        };
    }

    private static RaceSummary MapSummary(JsonElement race, DateTimeOffset fetchedAt)
    {
        return new RaceSummary
        {
            RaceId = ReadString(race, "raceId", "race_id", "id", "race.id", "_id") ?? "",
            Phase = NormalizePhase(ReadString(race, "phase", "status", "racePhase", "race.phase")),
            Pool = ReadDecimal(race, "pool", "race.pool", "currentGrossPool", "projectedGrossPool", "prizePool", "entryPool"),
            EntryFee = ReadDecimal(race, "entryFee", "entryFeeWei", "race.entryFee", "fee"),
            EntrantCount = ReadInt(race, "petCount", "entrantCount", "currentEntries", "entriesCount", "race.petCount"),
            MaxEntrants = ReadInt(race, "fieldSize", "maxEntrants", "field", "race.fieldSize"),
            LastFetchedAt = fetchedAt,
            RawJson = race.GetRawText()
        };
    }

    private static IReadOnlyList<string> ExtractResultOrder(JsonElement race)
    {
        var value = race.FindFirst("finalRanking", "resultOrder", "results", "ranking", "race.finalRanking");
        if (value is null || value.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.Value.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.Object
                ? ReadString(item, "petId", "id", "giglingId", "pet.id") ?? item.GetRawText()
                : item.GetStringValue() ?? item.GetRawText())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();
    }

    private static string? ReadString(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetStringValue();

    private static int? ReadInt(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetIntValue();

    private static decimal? ReadDecimal(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetDecimalValue();

    private static string NormalizePhase(string? phase)
    {
        if (string.IsNullOrWhiteSpace(phase))
        {
            return "Unknown";
        }

        return phase.Trim().ToUpperInvariant() switch
        {
            "0" or "IDLE" => "IDLE",
            "1" or "OPEN" => "OPEN",
            "2" or "RESOLVING" => "RESOLVING",
            "3" or "RESOLVED" => "RESOLVED",
            "4" or "CANCELLED" or "CANCELED" => "CANCELLED",
            var value => value
        };
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
}
