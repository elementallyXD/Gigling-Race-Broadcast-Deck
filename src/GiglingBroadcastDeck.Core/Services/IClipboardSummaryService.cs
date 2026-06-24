using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Creates short human-readable summaries from the currently selected public race data.
/// </summary>
public interface IClipboardSummaryService
{
    /// <summary>
    /// Formats a Discord-friendly summary without requiring every API field to be present.
    /// </summary>
    string CreateSummary(RaceSummary? summary, RaceDetail? detail);
}
