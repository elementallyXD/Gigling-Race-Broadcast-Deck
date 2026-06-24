using System.Globalization;
using System.Text.Json;

namespace GiglingBroadcastDeck.Core.Mapping;

/// <summary>
/// Defensive JSON helpers for public API payloads with evolving field names and shapes.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Looks up an object property without requiring exact casing.
    /// </summary>
    public static bool TryGetPropertyIgnoreCase(this JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Finds the first matching property path, including dotted nested paths such as <c>data.races</c>.
    /// </summary>
    public static JsonElement? FindFirst(this JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryFindByPath(element, propertyName, out var value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads string-like scalar JSON values without throwing on wrong types.
    /// </summary>
    public static string? GetStringValue(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    /// <summary>
    /// Reads an integer from a number or numeric string.
    /// </summary>
    public static int? GetIntValue(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }

    /// <summary>
    /// Reads a boolean from a boolean or boolean string.
    /// </summary>
    public static bool? GetBoolValue(this JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        if (element.ValueKind == JsonValueKind.String &&
            bool.TryParse(element.GetString(), out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Reads ISO date strings or Unix timestamps as UTC-aware values.
    /// </summary>
    public static DateTimeOffset? GetDateTimeOffsetValue(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var unix))
        {
            return unix > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
                : DateTimeOffset.FromUnixTimeSeconds(unix);
        }

        return null;
    }

    /// <summary>
    /// Reads decimal values and converts large integer-like wei amounts to ETH.
    /// </summary>
    public static decimal? GetDecimalValue(this JsonElement element)
    {
        decimal? value = element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetDecimal(out var parsedNumber) => parsedNumber,
            JsonValueKind.String when decimal.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedString) => parsedString,
            _ => null
        };

        if (value is not decimal number)
        {
            return null;
        }

        // Large integer-like values are likely wei. Convert to ETH for operator readability.
        return number > 1_000_000_000_000m ? number / 1_000_000_000_000_000_000m : number;
    }

    /// <summary>
    /// Treats common wrapper objects as arrays while returning an empty list for unusable shapes.
    /// </summary>
    public static IReadOnlyList<JsonElement> AsLikelyArray(this JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray().ToArray();
        }

        foreach (var propertyName in new[]
        {
            "races",
            "data",
            "items",
            "results",
            "raceList",
            "data.races",
            "data.items",
            "data.results",
            "payload.races",
            "payload.items"
        })
        {
            if (TryFindByPath(root, propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                return value.EnumerateArray().ToArray();
            }
        }

        return root.ValueKind == JsonValueKind.Object ? [root] : [];
    }

    /// <summary>
    /// Reads a decimal array while skipping malformed entries.
    /// </summary>
    public static IReadOnlyList<decimal> GetDecimalArrayValue(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray().Select(item => item.GetDecimalValue()).Where(item => item.HasValue).Select(item => item!.Value).ToArray()
            : [];

    /// <summary>
    /// Reads an integer array while skipping malformed entries.
    /// </summary>
    public static IReadOnlyList<int> GetIntArrayValue(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray().Select(item => item.GetIntValue()).Where(item => item.HasValue).Select(item => item!.Value).ToArray()
            : [];

    private static bool TryFindByPath(JsonElement element, string path, out JsonElement value)
    {
        value = element;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!value.TryGetPropertyIgnoreCase(segment, out value))
            {
                return false;
            }
        }

        return true;
    }
}
