using System.Text.Json;
using System.Text.Json.Nodes;

namespace Klavier.Config.UserSettings;

public class UserSettingsService : IUserSettingsService
{
    private const string _UserSettingsFileName = "usersettings.json";
    private const string _EmptyContent = "{}";
    // Settings changes are coalesced in memory and flushed to disk after this idle window.
    // Trade-off: if the app closes within this window of the last change, that change is lost.
    private const int _FlushDelayMs = 300;

    private static readonly JsonSerializerOptions _WriteOptions = new() { WriteIndented = true };

    private readonly Lock _lock = new();
    private readonly string _filePath;
    private readonly JsonObject _settings;
    private readonly Timer _flushTimer;

    public UserSettingsService(string appName)
    {
        _filePath = GetFilePath(appName);
        string json = File.ReadAllText(_filePath);
        _settings = JsonNode.Parse(json)?.AsObject() ?? [];
        _flushTimer = new Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
    }

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
        lock (_lock)
        {
            string[] segments = keyPath.Split(':');
            JsonObject current = _settings;
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
        }
        ScheduleFlush();
    }

    public void ClearSetting(string keyPath)
    {
        lock (_lock)
        {
            string[] segments = keyPath.Split(':');
            JsonObject current = _settings;
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
        }
        ScheduleFlush();
    }

    public void ResetAll()
    {
        lock (_lock)
        {
            _settings.Clear();
        }
        Flush();
    }

    private void ScheduleFlush()
    {
        _flushTimer.Change(_FlushDelayMs, Timeout.Infinite);
    }

    private void Flush()
    {
        string content;
        lock (_lock)
        {
            content = _settings.ToJsonString(_WriteOptions);
        }
        File.WriteAllText(_filePath, content);
    }
}
