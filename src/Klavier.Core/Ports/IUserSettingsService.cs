namespace Klavier.Core.Ports;

public interface IUserSettingsService
{
    void UpdateSetting(string sectionName, string key, object value);
    void ResetAll();
}
