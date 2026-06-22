using System.IO;
using System.Text.Json;

namespace GiglingBroadcastDeck.App.Services;

public sealed class OperatorPreferencesService : IOperatorPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GiglingBroadcastDeck",
        "operator-settings.json");

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
        catch
        {
            return new OperatorPreferences();
        }
    }

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
