namespace GiglingBroadcastDeck.Core.Services;

/// <summary>
/// Explains public Gigling Racing lifecycle phases in operator-friendly language.
/// </summary>
public interface IRacePhaseExplainer
{
    /// <summary>
    /// Converts a raw phase value from the public API into concise broadcast copy.
    /// </summary>
    /// <param name="phase">The normalized or raw phase value. Unknown values are safe.</param>
    /// <returns>A short explanation suitable for the WPF UI and OBS overlay.</returns>
    string Explain(string? phase);
}
