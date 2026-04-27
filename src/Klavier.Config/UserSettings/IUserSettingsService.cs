namespace Klavier.Config.UserSettings;

public interface IUserSettingsService
{
    void UpdateSetting(string keyPath, object value);
    void ClearSetting(string keyPath);
    void ResetAll();
}
