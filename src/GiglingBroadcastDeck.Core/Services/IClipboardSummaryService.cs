using GiglingBroadcastDeck.Core.Models;

namespace GiglingBroadcastDeck.Core.Services;

public interface IClipboardSummaryService
{
    string CreateSummary(RaceSummary? summary, RaceDetail? detail);
}
