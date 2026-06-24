namespace GiglingBroadcastDeck.Core.Models;

/// <summary>
/// Represents the outcome of one public API fetch without throwing network failures into callers.
/// </summary>
public sealed record ApiFetchResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorMessage,
    DateTimeOffset FetchedAt)
{
    /// <summary>
    /// Creates a successful fetch result with the raw or mapped value.
    /// </summary>
    public static ApiFetchResult<T> Success(T value, DateTimeOffset fetchedAt) =>
        new(true, value, null, fetchedAt);

    /// <summary>
    /// Creates a failed fetch result that preserves the attempted fetch timestamp.
    /// </summary>
    public static ApiFetchResult<T> Failure(string errorMessage, DateTimeOffset fetchedAt) =>
        new(false, default, errorMessage, fetchedAt);
}
