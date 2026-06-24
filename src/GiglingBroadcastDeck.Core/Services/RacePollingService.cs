using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Polls public race endpoints and maintains the app's last known safe race data.
/// </summary>
/// <remarks>
/// Overlapping requests are skipped through a lightweight gate. Invalid JSON and network
/// failures are converted into user-visible status rather than escaping into the WPF timer path.
/// </remarks>
public sealed class RacePollingService(
    IGigaverseRacingClient client,
    IRaceMapper mapper,
    ILogger<RacePollingService> logger) : IRacePollingService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, OwnerProfile?> _ownerProfileCache = new(StringComparer.OrdinalIgnoreCase);
    private RaceSummary? _selectedRace;

    /// <inheritdoc />
    public RaceDataSnapshot Snapshot { get; private set; } = new();

    /// <inheritdoc />
    public async Task<RaceDataSnapshot> RefreshRecentRacesAsync(CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return Snapshot;
        }

        try
        {
            var result = await client.GetRecentRacesRawAsync(cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                return MarkFailure(result.ErrorMessage ?? "Unable to fetch recent races.", result.FetchedAt);
            }

            IReadOnlyList<RaceSummary> races;
            try
            {
                races = mapper.MapRecentRaces(result.Value, result.FetchedAt);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Unable to parse recent races public API response.");
                return MarkFailure($"Unable to parse recent races response: {ex.Message}", result.FetchedAt);
            }

            Snapshot = Snapshot with
            {
                Races = races,
                Status = races.Count == 0 ? AppStatus.Empty : AppStatus.Ready,
                Message = races.Count == 0 ? "No recent races returned." : $"Loaded {races.Count} recent races.",
                IsStale = false,
                LastSuccessfulFetchAt = result.FetchedAt,
                LastAttemptedFetchAt = result.FetchedAt
            };

            return Snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RaceDataSnapshot> SelectRaceAsync(RaceSummary race, CancellationToken cancellationToken)
    {
        _selectedRace = race;
        return await RefreshSelectedRaceAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RaceDataSnapshot> RefreshSelectedRaceAsync(CancellationToken cancellationToken)
    {
        if (_selectedRace is null)
        {
            return Snapshot;
        }

        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return Snapshot;
        }

        try
        {
            var result = await client.GetRaceDetailRawAsync(_selectedRace.RaceId, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                return await TryLoadRaceStateFallbackAsync(
                    result.ErrorMessage ?? "Unable to fetch selected race.",
                    result.FetchedAt,
                    cancellationToken).ConfigureAwait(false);
            }

            RaceDetail detail;
            try
            {
                detail = mapper.MapRaceDetail(result.Value, _selectedRace.RaceId, result.FetchedAt);
                detail = await EnrichOwnerNamesAsync(detail, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Unable to parse selected race public API response for race {RaceId}.", _selectedRace.RaceId);
                return MarkFailure($"Unable to parse selected race response: {ex.Message}", result.FetchedAt);
            }

            Snapshot = Snapshot with
            {
                SelectedRaceDetail = detail,
                Status = AppStatus.Ready,
                Message = $"Loaded race #{detail.RaceId}.",
                IsStale = false,
                LastSuccessfulFetchAt = result.FetchedAt,
                LastAttemptedFetchAt = result.FetchedAt
            };

            return Snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<RaceDataSnapshot> TryLoadRaceStateFallbackAsync(
        string detailErrorMessage,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken)
    {
        if (_selectedRace is null)
        {
            return MarkFailure(detailErrorMessage, attemptedAt);
        }

        var fallback = await client.GetRaceStateRawAsync(_selectedRace.RaceId, cancellationToken).ConfigureAwait(false);
        if (!fallback.IsSuccess || fallback.Value is null)
        {
            return MarkFailure($"{detailErrorMessage} Fallback race-state also failed: {fallback.ErrorMessage}", fallback.FetchedAt);
        }

        RaceDetail detail;
        try
        {
            detail = mapper.MapRaceDetail(fallback.Value, _selectedRace.RaceId, fallback.FetchedAt);
            detail = await EnrichOwnerNamesAsync(detail, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Unable to parse race-state fallback public API response for race {RaceId}.", _selectedRace.RaceId);
            return MarkFailure($"Unable to parse race-state fallback response: {ex.Message}", fallback.FetchedAt);
        }

        Snapshot = Snapshot with
        {
            SelectedRaceDetail = detail,
            Status = AppStatus.Unavailable,
            Message = $"Race detail endpoint unavailable; showing race-state fallback. {detailErrorMessage}",
            IsStale = false,
            LastAttemptedFetchAt = fallback.FetchedAt
        };

        return Snapshot;
    }

    private RaceDataSnapshot MarkFailure(string message, DateTimeOffset attemptedAt)
    {
        Snapshot = Snapshot with
        {
            Status = Snapshot.LastSuccessfulFetchAt is null ? AppStatus.Error : AppStatus.Stale,
            Message = message,
            IsStale = Snapshot.LastSuccessfulFetchAt is not null,
            LastAttemptedFetchAt = attemptedAt
        };

        return Snapshot;
    }

    private async Task<RaceDetail> EnrichOwnerNamesAsync(RaceDetail detail, CancellationToken cancellationToken)
    {
        var ownerAddresses = detail.ResultEntrants
            .Concat(detail.LivePositions)
            .Select(entrant => entrant.OwnerAddress)
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ownerAddresses.Length == 0)
        {
            return detail;
        }

        foreach (var ownerAddress in ownerAddresses)
        {
            if (!_ownerProfileCache.ContainsKey(ownerAddress))
            {
                _ownerProfileCache[ownerAddress] = await TryGetOwnerProfileAsync(ownerAddress, cancellationToken).ConfigureAwait(false);
            }
        }

        return detail with
        {
            ResultEntrants = detail.ResultEntrants.Select(EnrichEntrantOwnerProfile).ToArray(),
            LivePositions = detail.LivePositions.Select(EnrichEntrantOwnerProfile).ToArray()
        };
    }

    private RaceEntrant EnrichEntrantOwnerProfile(RaceEntrant entrant)
    {
        if (string.IsNullOrWhiteSpace(entrant.OwnerAddress) ||
            !_ownerProfileCache.TryGetValue(entrant.OwnerAddress, out var profile) ||
            profile is null)
        {
            return entrant;
        }

        var topPetMatchesEntrant = !string.IsNullOrWhiteSpace(profile.TopPetId) &&
            (string.Equals(profile.TopPetId, entrant.PetId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.TopPetId, entrant.DisplayName, StringComparison.OrdinalIgnoreCase));

        return entrant with
        {
            OwnerName = string.IsNullOrWhiteSpace(entrant.OwnerName) ? profile.Username : entrant.OwnerName,
            OwnerHasNoob = entrant.OwnerHasNoob ?? profile.HasNoob,
            OwnerNoobId = entrant.OwnerNoobId ?? profile.NoobId,
            OwnerPetCount = entrant.OwnerPetCount ?? profile.PetCount,
            OwnerEnergy = entrant.OwnerEnergy ?? profile.Energy,
            OwnerMaxEnergy = entrant.OwnerMaxEnergy ?? profile.MaxEnergy,
            OwnerTopPetId = entrant.OwnerTopPetId ?? profile.TopPetId,
            OwnerTopPetName = entrant.OwnerTopPetName ?? profile.TopPetName,
            OwnerTopPetRarity = entrant.OwnerTopPetRarity ?? profile.TopPetRarity,
            OwnerTopPetGender = entrant.OwnerTopPetGender ?? profile.TopPetGender,
            OwnerTopPetElo = entrant.OwnerTopPetElo ?? profile.TopPetElo,
            PetRarity = entrant.PetRarity ?? (topPetMatchesEntrant ? profile.TopPetRarity : null),
            PetGender = entrant.PetGender ?? (topPetMatchesEntrant ? profile.TopPetGender : null),
            PetElo = entrant.PetElo ?? (topPetMatchesEntrant ? profile.TopPetElo : null)
        };
    }

    private async Task<OwnerProfile?> TryGetOwnerProfileAsync(string ownerAddress, CancellationToken cancellationToken)
    {
        var result = await client.GetNoobSummaryRawAsync(ownerAddress, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value))
        {
            logger.LogInformation(
                "Unable to resolve public username for owner {OwnerAddress}: {Message}",
                ownerAddress,
                result.ErrorMessage ?? "empty response");
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(result.Value);
            var root = document.RootElement;
            return new OwnerProfile
            {
                Username = ReadString(root, "summary.username", "username", "data.username"),
                HasNoob = ReadBool(root, "summary.hasNoob", "hasNoob", "data.hasNoob"),
                NoobId = ReadInt(root, "summary.noobId", "noobId", "data.noobId"),
                PetCount = ReadInt(root, "summary.petCount", "petCount", "data.petCount"),
                Energy = ReadInt(root, "summary.energyValue", "energyValue", "data.energyValue"),
                MaxEnergy = ReadInt(root, "summary.maxEnergy", "maxEnergy", "data.maxEnergy"),
                TopPetId = ReadString(root, "summary.topRacingGigling.petId", "topRacingGigling.petId"),
                TopPetName = ReadString(root, "summary.topRacingGigling.name", "topRacingGigling.name"),
                TopPetRarity = NormalizeRarity(ReadString(root, "summary.topRacingGigling.rarity", "topRacingGigling.rarity")),
                TopPetGender = ReadString(root, "summary.topRacingGigling.gender", "topRacingGigling.gender"),
                TopPetElo = ReadDecimal(root, "summary.topRacingGigling.elo", "topRacingGigling.elo")
            };
        }
        catch (JsonException ex)
        {
            logger.LogInformation(ex, "Unable to parse public username response for owner {OwnerAddress}.", ownerAddress);
            return null;
        }
    }

    private sealed record OwnerProfile
    {
        public string? Username { get; init; }
        public bool? HasNoob { get; init; }
        public int? NoobId { get; init; }
        public int? PetCount { get; init; }
        public int? Energy { get; init; }
        public int? MaxEnergy { get; init; }
        public string? TopPetId { get; init; }
        public string? TopPetName { get; init; }
        public string? TopPetRarity { get; init; }
        public string? TopPetGender { get; init; }
        public decimal? TopPetElo { get; init; }
    }

    private static string? ReadString(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (TryReadString(root, path, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? ReadInt(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!TryReadValue(root, path, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!TryReadValue(root, path, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return null;
    }

    private static bool? ReadBool(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!TryReadValue(root, path, out var value))
            {
                continue;
            }

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? NormalizeRarity(string? value) =>
        value?.Trim() switch
        {
            "0" => "Common",
            "1" => "Uncommon",
            "2" => "Rare",
            "3" => "Epic",
            "4" => "Legendary",
            "5" => "Relic",
            "6" => "Giga",
            "" or null => null,
            var text => text
        };

    private static bool TryReadString(JsonElement root, string path, out string? value)
    {
        if (!TryReadValue(root, path, out var current))
        {
            value = null;
            return false;
        }

        value = current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };

        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadValue(JsonElement root, string path, out JsonElement value)
    {
        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current.ValueKind != JsonValueKind.Object || !TryGetPropertyIgnoreCase(current, segment, out current))
            {
                value = default;
                return false;
            }
        }

        value = current;
        return true;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
