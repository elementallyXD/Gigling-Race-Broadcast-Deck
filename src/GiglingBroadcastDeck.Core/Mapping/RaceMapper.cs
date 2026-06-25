using System.Text.Json;
using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Mapping;

/// <summary>
/// Tolerant mapper for public Gigling Racing JSON payloads.
/// </summary>
/// <remarks>
/// The mapper accepts multiple possible field names because hackathon APIs and indexers can
/// change response shapes. Malformed JSON is allowed to throw <see cref="JsonException"/> so
/// polling services can mark data stale and preserve the last good snapshot.
/// </remarks>
public sealed class RaceMapper : IRaceMapper
{
    /// <inheritdoc />
    public IReadOnlyList<RaceSummary> MapRecentRaces(string rawJson, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(rawJson);

        return document.RootElement
            .AsLikelyArray()
            .Select(item => MapSummary(item, fetchedAt))
            .Where(race => !string.IsNullOrWhiteSpace(race.RaceId))
            .ToArray();
    }

    /// <inheritdoc />
    public RaceDetail MapRaceDetail(string rawJson, string raceId, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var race = root.FindFirst("race", "data.race", "data") ?? root;

        var summary = MapSummary(race, fetchedAt);
        var resultEntrants = FirstNonEmptyEntrants(
            ExtractEntrants(false, race, "finalRanking", "resultOrder", "results", "ranking", "race.finalRanking"),
            ExtractEntrants(false, root, "finalRanking", "resultOrder", "results", "ranking", "race.finalRanking", "data.race.finalRanking", "data.resultOrder"));
        var referenceEntrants = MergeEntrants(
            ExtractEntrants(
                false,
                race,
                "entrants",
                "pets",
                "participants",
                "players",
                "registeredPets",
                "entries",
                "race.entrants",
                "race.pets",
                "race.participants",
                "race.entries"),
            ExtractPetOwners(race),
            ExtractEntrants(
                false,
                root,
                "entrants",
                "pets",
                "participants",
                "players",
                "registeredPets",
                "entries",
                "race.entrants",
                "race.pets",
                "race.participants",
                "race.entries",
                "data.entrants",
                "data.pets",
                "data.participants",
                "data.players",
                "data.registeredPets",
                "data.entries"),
            ExtractPetOwners(root));
        var livePositions = FirstNonEmptyEntrants(
            ExtractEntrants(true, race, "livePositions", "currentPositions", "positions", "standings", "entrants", "pets", "race.positions"),
            ExtractEntrants(true, root, "livePositions", "currentPositions", "positions", "standings", "race.positions", "data.race.positions", "data.livePositions", "data.currentPositions"));
        var finishTimes = FirstNonEmptyIntArray(
            ReadIntArray(race, "finishTimes", "race.finishTimes"),
            ReadIntArray(root, "finishTimes", "race.finishTimes", "data.finishTimes", "data.race.finishTimes"));
        var enrichedResultEntrants = ApplyFinishTimes(EnrichEntrants(resultEntrants, referenceEntrants.Concat(livePositions).ToArray()), finishTimes);
        var resultOrder = enrichedResultEntrants.Select(entrant => entrant.DisplayName).ToArray();

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
            RaceType = summary.RaceType,
            Source = summary.Source,
            ResultOrder = resultOrder,
            ResultEntrants = enrichedResultEntrants,
            LivePositions = livePositions,
            LastFetchedAt = fetchedAt,
            RawJson = rawJson
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<RaceSummary> MapScheduledRaces(string rawJson, DateTimeOffset fetchedAt) =>
        MapRecentRaces(rawJson, fetchedAt);

    /// <inheritdoc />
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

    /// <inheritdoc />
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
            Weather = NormalizeParamValue("weather", ReadString(race, "weather", "trackCondition", "raceTemp", "temp", "temperature", "extraParams.weather", "raceParams.weather", "raceParams.temp")),
            RaceType = NormalizeRaceType(
                ReadString(race, "raceType", "race.type", "type", "source.displayName", "source.type", "dataSource", "joinHookPolicy.kind", "joinHook.kind"),
                ReadBool(race, "isPrivate", "private", "race.isPrivate")),
            Source = ReadString(race, "source.type", "source.displayName", "source", "dataSource"),
            LastFetchedAt = fetchedAt,
            RawJson = race.GetRawText()
        };
    }

    private static IReadOnlyList<RaceEntrant> ExtractEntrants(bool sortAsLivePositions, JsonElement race, params string[] fieldNames)
    {
        var value = race.FindFirst(fieldNames);
        if (value is null || value.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entrants = value.Value.EnumerateArray()
            .Select((item, index) => MapEntrant(item, index, assignFallbackPlace: !sortAsLivePositions))
            .Where(entrant => !string.IsNullOrWhiteSpace(entrant.DisplayName))
            .ToArray();

        return sortAsLivePositions
            ? entrants
                .OrderBy(entrant => entrant.Place ?? int.MaxValue)
                .ThenByDescending(entrant => entrant.Progress ?? decimal.MinValue)
                .ThenByDescending(entrant => entrant.Position ?? decimal.MinValue)
                .ToArray()
            : entrants
                .OrderBy(entrant => entrant.Place ?? int.MaxValue)
                .ToArray();
    }

    private static IReadOnlyList<RaceEntrant> FirstNonEmptyEntrants(params IReadOnlyList<RaceEntrant>[] entrantLists) =>
        entrantLists.FirstOrDefault(entrants => entrants.Count > 0) ?? [];

    private static IReadOnlyList<int> FirstNonEmptyIntArray(params IReadOnlyList<int>[] valueLists) =>
        valueLists.FirstOrDefault(values => values.Count > 0) ?? [];

    private static IReadOnlyList<RaceEntrant> MergeEntrants(params IReadOnlyList<RaceEntrant>[] entrantLists) =>
        entrantLists.SelectMany(entrants => entrants).ToArray();

    private static IReadOnlyList<RaceEntrant> ExtractPetOwners(JsonElement race)
    {
        var value = race.FindFirst("petOwners", "race.petOwners", "data.petOwners", "data.race.petOwners");
        if (value is null || value.Value.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return value.Value.EnumerateObject()
            .Select(property => new RaceEntrant
            {
                DisplayName = property.Name,
                PetId = property.Name,
                OwnerAddress = property.Value.GetStringValue()
            })
            .Where(entrant => !string.IsNullOrWhiteSpace(entrant.OwnerAddress))
            .ToArray();
    }

    private static RaceEntrant MapEntrant(JsonElement item, int index, bool assignFallbackPlace)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            var text = item.GetStringValue() ?? item.GetRawText();
            return new RaceEntrant
            {
                DisplayName = string.IsNullOrWhiteSpace(text) ? $"Entrant {index + 1}" : text,
                Place = assignFallbackPlace ? index + 1 : null
            };
        }

        var petId = ReadString(item, "petId", "id", "giglingId", "tokenId", "pet.id", "pet.petId", "pet.giglingId");
        var petName = ReadString(item, "name", "petName", "giglingName", "displayName", "pet.name", "pet.petName", "pet.giglingName");
        var petRarity = ReadString(item, "rarity", "petRarity", "pet.rarity");
        var petGender = ReadString(item, "gender", "petGender", "pet.gender");
        var petElo = ReadDecimal(item, "elo", "rating", "petElo", "pet.elo", "pet.rating");
        var ownerName = ReadString(
            item,
            "ownerName",
            "ownerNickname",
            "ownerUsername",
            "playerName",
            "playerNickname",
            "playerUsername",
            "username",
            "nickname",
            "userName",
            "discordName",
            "discordUsername",
            "player.name",
            "player.nickname",
            "player.username",
            "player.displayName",
            "owner.name",
            "owner.nickname",
            "owner.username",
            "owner.displayName",
            "user.name",
            "user.nickname",
            "user.username",
            "user.displayName",
            "profile.nickname",
            "profile.username",
            "account.nickname",
            "account.username");
        var ownerAddress = ReadString(
            item,
            "owner",
            "ownerAddress",
            "address",
            "wallet",
            "walletAddress",
            "player",
            "player.address",
            "player.wallet",
            "owner.address",
            "owner.wallet",
            "user.address",
            "user.wallet");
        var place = ReadInt(item, "place", "rank", "positionRank", "finishPosition") ?? (assignFallbackPlace ? index + 1 : null);
        var position = ReadDecimal(item, "position", "currentPosition", "x", "distance", "meters", "distanceTravelled");
        var progress = ReadDecimal(item, "progress", "percent", "completion", "distancePercent");
        var slot = ReadInt(item, "slot", "entrySlot", "lane");
        var joinedAt = ReadDate(item, "joinedAt", "entryTime", "createdAt");
        var isJuiced = ReadBool(item, "juiced", "isJuiced");
        var protoSurcharge = ReadDecimal(item, "protoSurcharge", "surcharge", "entrySurcharge");
        var displayName = FirstNonEmpty(petName ?? "", petId ?? "", $"Entrant {index + 1}");

        return new RaceEntrant
        {
            DisplayName = displayName,
            PetId = petId,
            PetRarity = petRarity,
            PetGender = petGender,
            PetElo = petElo,
            OwnerName = ownerName,
            OwnerAddress = ownerAddress,
            Slot = slot,
            JoinedAt = joinedAt,
            IsJuiced = isJuiced,
            ProtoSurcharge = protoSurcharge,
            Place = place,
            Position = position,
            Progress = progress
        };
    }

    private static IReadOnlyList<RaceEntrant> EnrichEntrants(IReadOnlyList<RaceEntrant> entrants, IReadOnlyList<RaceEntrant> references)
    {
        if (entrants.Count == 0 || references.Count == 0)
        {
            return entrants;
        }

        return entrants
            .Select(entrant => EnrichEntrant(entrant, references))
            .ToArray();
    }

    private static RaceEntrant EnrichEntrant(RaceEntrant entrant, IReadOnlyList<RaceEntrant> references)
    {
        var reference = references.FirstOrDefault(candidate => EntrantsMatch(entrant, candidate));
        if (reference is null)
        {
            return entrant;
        }

        return entrant with
        {
            DisplayName = ShouldUseReferenceDisplayName(entrant, reference) ? reference.DisplayName : entrant.DisplayName,
            PetId = FirstNonEmptyOrNull(entrant.PetId, reference.PetId),
            PetRarity = FirstNonEmptyOrNull(entrant.PetRarity, reference.PetRarity),
            PetGender = FirstNonEmptyOrNull(entrant.PetGender, reference.PetGender),
            PetElo = entrant.PetElo ?? reference.PetElo,
            OwnerName = FirstNonEmptyOrNull(entrant.OwnerName, reference.OwnerName),
            OwnerAddress = FirstNonEmptyOrNull(entrant.OwnerAddress, reference.OwnerAddress),
            Slot = entrant.Slot ?? reference.Slot,
            JoinedAt = entrant.JoinedAt ?? reference.JoinedAt,
            IsJuiced = entrant.IsJuiced ?? reference.IsJuiced,
            ProtoSurcharge = entrant.ProtoSurcharge ?? reference.ProtoSurcharge,
            Position = entrant.Position ?? reference.Position,
            Progress = entrant.Progress ?? reference.Progress
        };
    }

    private static IReadOnlyList<RaceEntrant> ApplyFinishTimes(IReadOnlyList<RaceEntrant> entrants, IReadOnlyList<int> finishTimes) =>
        finishTimes.Count == 0
            ? entrants
            : entrants.Select((entrant, index) => entrant with
            {
                FinishTimeMs = index < finishTimes.Count ? finishTimes[index] : entrant.FinishTimeMs
            }).ToArray();

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

    private static string? FirstNonEmptyOrNull(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? ReadString(JsonElement race, params string[] names)
    {
        foreach (var name in names)
        {
            var value = race.FindFirst(name)?.GetStringValue();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

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
            "weather" => value.Trim() switch
            {
                "0" => "Hot",
                "1" => "Cold",
                "2" => "Rainy",
                "3" => "Snowing",
                var text => HumanizeWeather(text)
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

    private static string HumanizeWeather(string value)
    {
        var text = value.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? text
            : char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();
    }

    private static string? NormalizeRaceType(string? value, bool? isPrivate)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? null
            : HumanizeRaceType(value);

        if (!string.IsNullOrWhiteSpace(normalized) &&
            !string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return isPrivate switch
        {
            true => "Private",
            false => "Public",
            _ => null
        };
    }

    private static string HumanizeRaceType(string value)
    {
        var words = value.Replace("_", " ").Replace("-", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words.Length == 0
            ? value
            : string.Join(" ", words.Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }
}
