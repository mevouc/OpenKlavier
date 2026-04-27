using System.Text.Json;
using System.Text.Json.Nodes;

namespace Klavier.Config.UserSettings;

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

    public void UpdateSetting(string keyPath, object value)
    {
        string json = File.ReadAllText(_filePath);
        JsonObject root = JsonNode.Parse(json)?.AsObject() ?? [];

        string[] segments = keyPath.Split(':');
        JsonObject current = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            JsonObject? child = current[segments[i]]?.AsObject();
            if (child is null)
            {
                child = [];
                current[segments[i]] = child;
            }
            current = child;
        }
        current[segments[^1]] = JsonSerializer.SerializeToNode(value);

        File.WriteAllText(_filePath, root.ToJsonString(_WriteOptions));
    }

    public void ClearSetting(string keyPath)
    {
        string json = File.ReadAllText(_filePath);
        JsonObject? root = JsonNode.Parse(json)?.AsObject();
        if (root is null)
        {
            return;
        }

        string[] segments = keyPath.Split(':');
        JsonObject current = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            JsonObject? child = current[segments[i]]?.AsObject();
            if (child is null)
            {
                return; // path doesn't exist, nothing to clear
            }
            current = child;
        }
        current.Remove(segments[^1]);

        File.WriteAllText(_filePath, root.ToJsonString(_WriteOptions));
    }

    public void ResetAll()
    {
        File.WriteAllText(_filePath, _EmptyContent);
    }
}
