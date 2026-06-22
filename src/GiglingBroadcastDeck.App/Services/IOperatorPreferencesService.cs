namespace GiglingBroadcastDeck.App.Services;

public interface IOperatorPreferencesService
{
    OperatorPreferences Load();

    void Save(OperatorPreferences preferences);
}
