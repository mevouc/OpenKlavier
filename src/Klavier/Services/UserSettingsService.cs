using System.Text.Json;
using System.Text.Json.Nodes;
using Klavier.Core.Ports;

namespace Klavier.Services;

public class UserSettingsService(string appName) : IUserSettingsService
{
    private const string _UserSettingsFileName = "usersettings.json";
    private const string _EmptyContent = "{}";

    private static readonly JsonSerializerOptions _WriteOptions = new() { WriteIndented = true };

    private readonly string _filePath = GetFilePath(appName);

    public static string GetFilePath(string appName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            _UserSettingsFileName);
    }

    public static void EnsureCreated(string appName)
    {
        string filePath = GetFilePath(appName);

        if (!File.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, _EmptyContent);
        }
    }

    public void UpdateSetting(string sectionName, string key, object value)
    {
        string json = File.ReadAllText(_filePath);
        JsonNode root = JsonNode.Parse(json) ?? new JsonObject();

        JsonObject section = root[sectionName]?.AsObject() ?? [];
        section[key] = JsonSerializer.SerializeToNode(value);
        root[sectionName] = section;

        File.WriteAllText(_filePath, root.ToJsonString(_WriteOptions));
    }

    public void ResetAll()
    {
        File.WriteAllText(_filePath, _EmptyContent);
    }
}
