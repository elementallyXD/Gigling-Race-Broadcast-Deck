namespace GiglingBroadcastDeck.Core.Models;

public sealed record ApiFetchResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorMessage,
    DateTimeOffset FetchedAt)
{
    public static ApiFetchResult<T> Success(T value, DateTimeOffset fetchedAt) =>
        new(true, value, null, fetchedAt);

    public static ApiFetchResult<T> Failure(string errorMessage, DateTimeOffset fetchedAt) =>
        new(false, default, errorMessage, fetchedAt);
}
