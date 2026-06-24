namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Default lifecycle explainer for the race phases used by Gigling Broadcast Deck.
/// </summary>
/// <remarks>
/// The explainer intentionally accepts unknown values because the public API can evolve
/// during the hackathon. Unknown phases should inform the operator, not crash the app.
/// </remarks>
public sealed class RacePhaseExplainer : IRacePhaseExplainer
{
    /// <inheritdoc />
    public string Explain(string? phase)
    {
        return (phase ?? "Unknown").Trim().ToUpperInvariant() switch
        {
            "IDLE" => "IDLE: race is inactive or not created.",
            "OPEN" => "OPEN: race is accepting entrants.",
            "RESOLVING" => "RESOLVING: field is full and waiting for final resolution.",
            "RESOLVED" => "RESOLVED: final result is available.",
            "CANCELLED" => "CANCELLED: race is cancelled and refund state may apply.",
            _ => "Unknown: this phase was not recognized by the app."
        };
    }
}
