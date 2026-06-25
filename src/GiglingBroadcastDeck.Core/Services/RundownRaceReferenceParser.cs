namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Extracts race identifiers from operator-created rundown lines.
/// </summary>
/// <remarks>
/// Only lines created by the "Pin Selected Race" workflow are treated as selectable race
/// references. Free-form ticker lines are intentionally ignored.
/// </remarks>
public static class RundownRaceReferenceParser
{
    private const string RacePrefix = "Race #";

    /// <summary>
    /// Attempts to read a race identifier from a pinned rundown line.
    /// </summary>
    /// <param name="rundownItem">The operator rundown item text.</param>
    /// <returns>The race identifier when the item references a race; otherwise <see langword="null" />.</returns>
    public static string? TryExtractRaceId(string? rundownItem)
    {
        if (string.IsNullOrWhiteSpace(rundownItem))
        {
            return null;
        }

        var text = rundownItem.Trim();
        if (!text.StartsWith(RacePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var raceIdStart = RacePrefix.Length;
        var separatorIndex = text.IndexOf(" - ", raceIdStart, StringComparison.Ordinal);
        var raceId = separatorIndex >= 0
            ? text[raceIdStart..separatorIndex]
            : text[raceIdStart..];

        return string.IsNullOrWhiteSpace(raceId) ? null : raceId.Trim();
    }
}
