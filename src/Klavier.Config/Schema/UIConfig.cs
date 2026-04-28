namespace Klavier.Config.Schema;

public class UIConfig
{
    public const string SectionName = "UI";

    public AppTheme Theme { get; init; } = AppTheme.Dark;
    public bool Topmost { get; init; }
    public bool ShowKeyLabels { get; init; } = true;
    public bool ShowNoteLabels { get; init; } = true;
    public NoteNameStyle NoteNameStyle { get; init; } = NoteNameStyle.Scientific;
    public SustainMode SustainMode { get; init; } = SustainMode.Hold;
    public string KeyboardLayout { get; init; } = "qwerty";
    public ColorsConfig Colors { get; init; } = new();

    public static class Keys
    {
        public static readonly string Theme = ConfigKey.Of(SectionName, nameof(UIConfig.Theme));
        public static readonly string Topmost = ConfigKey.Of(SectionName, nameof(UIConfig.Topmost));
        public static readonly string ShowKeyLabels = ConfigKey.Of(SectionName, nameof(UIConfig.ShowKeyLabels));
        public static readonly string ShowNoteLabels = ConfigKey.Of(SectionName, nameof(UIConfig.ShowNoteLabels));
        public static readonly string NoteNameStyle = ConfigKey.Of(SectionName, nameof(UIConfig.NoteNameStyle));
        public static readonly string SustainMode = ConfigKey.Of(SectionName, nameof(UIConfig.SustainMode));
        public static readonly string KeyboardLayout = ConfigKey.Of(SectionName, nameof(UIConfig.KeyboardLayout));

        public static class Colors
        {
            public static readonly string Accent = ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.Accent));
            public static readonly string WhiteKey = ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.WhiteKey));
            public static readonly string BlackKey = ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.BlackKey));
            public static readonly string KeyBorder = ConfigKey.Of(UIConfig.SectionName, nameof(UIConfig.Colors), nameof(ColorsConfig.KeyBorder));
        }
    }
}
