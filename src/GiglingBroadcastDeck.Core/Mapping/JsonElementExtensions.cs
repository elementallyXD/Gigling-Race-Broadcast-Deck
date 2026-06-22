using System.Globalization;
using System.Text.Json;

namespace GiglingBroadcastDeck.Core.Mapping;

public static class JsonElementExtensions
{
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
