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
            EntryFee = summary.EntryFee,
            EntrantCount = summary.EntrantCount,
            MaxEntrants = summary.MaxEntrants,
            TrackLength = summary.TrackLength,
            Creator = summary.Creator,
            IsPrivate = summary.IsPrivate,
            RaceStart = summary.RaceStart,
            RaceFinish = summary.RaceFinish,
            CurrentNetPrizePool = summary.CurrentNetPrizePool,
            ProjectedNetPrizePool = summary.ProjectedNetPrizePool,
            CurrentPayouts = summary.CurrentPayouts,
            ProjectedPayouts = summary.ProjectedPayouts,
            PayoutDistribution = summary.PayoutDistribution,
            Weather = summary.Weather,
            Faction = summary.Faction,
            ItemsMode = summary.ItemsMode,
            Source = summary.Source,
            ResultOrder = resultOrder,
            LastFetchedAt = fetchedAt,
            RawJson = rawJson
        };
    }

    public IReadOnlyList<RaceSummary> MapScheduledRaces(string rawJson, DateTimeOffset fetchedAt) =>
        MapRecentRaces(rawJson, fetchedAt);

    public IReadOnlyList<StatLine> MapGlobalStats(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement.FindFirst("stats", "data", "payload") ?? document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return root.EnumerateObject()
            .Where(property => property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            .Select(property => new StatLine
            {
                Label = Humanize(property.Name),
                Value = property.Value.GetStringValue() ?? property.Value.GetRawText()
            })
            .ToArray();
    }

    public IReadOnlyList<LeaderboardEntry> MapLeaderboard(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);

        return document.RootElement
            .AsLikelyArray()
            .Select((item, index) => new LeaderboardEntry
            {
                Rank = ReadInt(item, "rank", "position", "place") ?? index + 1,
                Name = ReadString(item, "name", "petName", "giglingName", "username", "ownerName") ?? "Unknown",
                PetId = ReadString(item, "petId", "giglingId", "id", "pet.id"),
                Owner = ReadString(item, "owner", "ownerAddress", "address", "player"),
                Faction = NormalizeParamValue("faction", ReadString(item, "faction", "pet.faction")),
                Rarity = ReadString(item, "rarity", "pet.rarity"),
                Elo = ReadDecimal(item, "elo", "rating", "score")
            })
            .ToArray();
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
            TrackLength = ReadInt(race, "trackLength", "race.trackLength", "distance", "meters"),
            Creator = ReadString(race, "creator", "host", "race.creator"),
            IsPrivate = ReadBool(race, "isPrivate", "private", "race.isPrivate"),
            RaceStart = ReadDate(race, "raceStart", "startedAt", "createdAt", "race.raceStart"),
            RaceFinish = ReadDate(race, "raceFinish", "finishedAt", "resolvedAt", "race.raceFinish"),
            CurrentNetPrizePool = ReadDecimal(race, "currentNetPrizePool", "payoutPreview.currentNetPrizePool", "preview.currentNetPrizePool"),
            ProjectedNetPrizePool = ReadDecimal(race, "projectedNetPrizePool", "payoutPreview.projectedNetPrizePool", "preview.projectedNetPrizePool"),
            CurrentPayouts = ReadDecimalArray(race, "currentPayouts", "payoutPreview.currentPayouts", "preview.currentPayouts"),
            ProjectedPayouts = ReadDecimalArray(race, "projectedPayouts", "payoutPreview.projectedPayouts", "preview.projectedPayouts"),
            PayoutDistribution = ReadIntArray(race, "payoutDistribution", "race.payoutDistribution"),
            Weather = NormalizeParamValue("weather", ReadString(race, "weather", "trackCondition", "extraParams.weather")),
            Faction = NormalizeParamValue("faction", ReadString(race, "faction", "extraParams.faction")),
            ItemsMode = NormalizeParamValue("items", ReadString(race, "items", "itemsMode", "extraParams.items")),
            Source = ReadString(race, "source", "dataSource"),
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

    private static bool? ReadBool(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetBoolValue();

    private static decimal? ReadDecimal(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetDecimalValue();

    private static DateTimeOffset? ReadDate(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetDateTimeOffsetValue();

    private static IReadOnlyList<decimal> ReadDecimalArray(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetDecimalArrayValue() ?? [];

    private static IReadOnlyList<int> ReadIntArray(JsonElement race, params string[] names) =>
        race.FindFirst(names)?.GetIntArrayValue() ?? [];

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

    private static string Humanize(string value)
    {
        var chars = value.SelectMany((character, index) =>
            index > 0 && char.IsUpper(character) ? new[] { ' ', character } : new[] { character });

        return string.Concat(chars).Replace("_", " ").Trim();
    }

    private static string? NormalizeParamValue(string paramName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return paramName switch
        {
            "items" => value.Trim() switch
            {
                "0" => "None",
                "1" => "Dung",
                "2" => "Butterflies",
                "3" => "All",
                var text => text
            },
            "weather" => value.Trim() switch
            {
                "0" => "Hot",
                "1" => "Cold",
                "2" => "Rainy",
                "3" => "Snowing",
                var text => text
            },
            "faction" => value.Trim() switch
            {
                "0" => "None",
                "1" => "Crusader",
                "2" => "Overseer",
                "3" => "Athena",
                "4" => "Archon",
                "5" => "Foxglove",
                "6" => "Summoner",
                "7" => "Chobo",
                "8" => "Gigus",
                var text => text
            },
            _ => value
        };
    }
}
