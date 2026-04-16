namespace Klavier.UI.Ports;

public interface IUserSettingsService
{
    void UpdateSetting(string keyPath, object value);
    void ResetAll();
}
