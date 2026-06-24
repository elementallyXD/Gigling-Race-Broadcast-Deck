using GiglingBroadcastDeck.Core.Services;

namespace GiglingBroadcastDeck.Tests;

public sealed class RacePhaseExplainerTests
{
    [Theory]
    [InlineData("open", "OPEN: race is accepting entrants.")]
    [InlineData("RESOLVING", "RESOLVING: field is full and waiting for final resolution.")]
    [InlineData("resolved", "RESOLVED: final result is available.")]
    public void Explain_ReturnsBroadcastFriendlyTextForKnownPhases(string phase, string expected)
    {
        var explainer = new RacePhaseExplainer();

        var explanation = explainer.Explain(phase);

        Assert.Equal(expected, explanation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("future-phase")]
    public void Explain_ReturnsSafeTextForUnknownPhases(string? phase)
    {
        var explainer = new RacePhaseExplainer();

        var explanation = explainer.Explain(phase);

        Assert.Contains("Unknown", explanation);
    }
}
