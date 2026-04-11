namespace Klavier.UI.Ports;

public interface IUserSettingsService
{
    void UpdateSetting(string sectionName, string key, object value);
    void ResetAll();
}
