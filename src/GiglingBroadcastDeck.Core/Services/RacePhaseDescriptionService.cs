namespace GiglingBroadcastDeck.Core.Services;

public static class RacePhaseDescriptionService
{
    public static string Describe(string? phase)
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
