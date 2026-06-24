using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GiglingBroadcastDeck.App.Services;

/// <summary>
/// Persists operator preferences as a small JSON file in the user's local app data folder.
/// </summary>
public sealed class OperatorPreferencesService(ILogger<OperatorPreferencesService> logger) : IOperatorPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GiglingBroadcastDeck",
        "operator-settings.json");

    /// <inheritdoc />
    public OperatorPreferences Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new OperatorPreferences();
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<OperatorPreferences>(json, JsonOptions) ?? new OperatorPreferences();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to load operator preferences from {SettingsPath}; using defaults.", _settingsPath);
            return new OperatorPreferences();
        }
    }

    /// <inheritdoc />
    public void Save(OperatorPreferences preferences)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(preferences, JsonOptions));
    }
}
