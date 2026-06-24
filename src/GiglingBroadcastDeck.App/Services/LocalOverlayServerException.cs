namespace GiglingBroadcastDeck.App.Services;

/// <summary>
/// Represents a user-safe local overlay server startup failure.
/// </summary>
public sealed class LocalOverlayServerException(string message, Exception innerException)
    : Exception(message, innerException);
