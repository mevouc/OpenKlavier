namespace Klavier.Config;

public static class ConfigKey
{
    public static string Of(params string[] segments) => string.Join(':', segments);
}
