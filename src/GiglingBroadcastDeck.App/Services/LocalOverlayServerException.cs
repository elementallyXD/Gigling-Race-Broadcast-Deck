namespace GiglingBroadcastDeck.App.Services;

public sealed class LocalOverlayServerException(string message, Exception innerException)
    : Exception(message, innerException);
