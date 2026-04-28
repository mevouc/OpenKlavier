using Klavier.Config.Schema;
using Klavier.SoundFont.Parsing;
using Klavier.SoundFont.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klavier.SoundFont.Loading;

/// <summary>
/// Owns the parsed <see cref="SoundFontInfo"/> for the currently active SoundFont. Single source of
/// truth: parses once per path change and exposes the result to all consumers via
/// <see cref="ISoundFontInfoProvider"/>. The file loader pushes updates through <see cref="TryReload"/>;
/// external config edits are picked up via <see cref="IOptionsMonitor{T}"/>.
/// </summary>
public class SoundFontInfoCache : ISoundFontInfoProvider, IDisposable
{
    private static readonly SoundFontInfo _Empty = new(null, new Dictionary<(int Bank, int Program), SoundFontPreset>());

    private readonly ILogger<SoundFontInfoCache> _logger;
    private readonly IDisposable? _configSubscription;

    private SoundFontInfo _info = _Empty;
    private string? _cachedPath;

    public event Action? SoundFontInfoChanged;

    public SoundFontInfoCache(IOptionsMonitor<AudioConfig> audioConfig, ILogger<SoundFontInfoCache> logger)
    {
        _logger = logger;

        // Prime the cache from the initial config (startup path).
        TryReload(audioConfig.CurrentValue.SoundFont.Path);

        // Pick up external edits to the user-settings file. TryReload no-ops when the path is already
        // cached, so the loader's own TryReload + subsequent settings-write flow only parses once.
        _configSubscription = audioConfig.OnChange(c => TryReload(c.SoundFont.Path));
    }

    public SoundFontInfo GetSoundFontInfo() => _info;

    /// <summary>
    /// Parses <paramref name="path"/> if it differs from the cached path and updates the cache.
    /// Returns true on success (or when the path is already cached), false on parse failure.
    /// </summary>
    public bool TryReload(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        if (path == _cachedPath)
        {
            return true;
        }

        SoundFontInfo parsed;
        try
        {
            parsed = SoundFontParser.ParseInfo(path);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Failed to parse SoundFont file {Path}", path);
            return false;
        }

        _info = parsed;
        _cachedPath = path;
        SoundFontInfoChanged?.Invoke();
        return true;
    }

    public void Dispose()
    {
        _configSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
