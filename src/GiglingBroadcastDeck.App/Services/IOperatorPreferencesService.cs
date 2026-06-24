namespace GiglingBroadcastDeck.App.Services;

/// <summary>
/// Loads and saves local operator preferences that do not contain secrets or race data.
/// </summary>
public interface IOperatorPreferencesService
{
    /// <summary>
    /// Loads persisted preferences or safe defaults if the file is missing/corrupt.
    /// </summary>
    OperatorPreferences Load();

    /// <summary>
    /// Saves local overlay and rundown preferences.
    /// </summary>
    void Save(OperatorPreferences preferences);
}
