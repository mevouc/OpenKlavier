using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.Core.Engine;
using Klavier.Core.Music;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Toolbar;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views.Settings.KeybindsEditor;

public class KeybindsEditorWindow : Window
{
    private const string _WindowTitle = "Keybinds Editor";
    private const int _DefaultWidth = 800;
    private const int _DefaultHeight = 500;
    private const int _MinWidth = 500;
    private const int _MinHeight = 450;
    private const string _ModifierLabel = "Modifier for black keys:";
    private const string _BackButtonLabel = "Back";
    private const string _SkipButtonLabel = "Skip";
    private const string _SaveButtonLabel = "Save";
    private const double _StatusFontSize = 20;

    private static readonly string[] _ModifierOptions = ["Shift", "Ctrl", "Alt"];
    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);
    private static readonly SolidColorBrush _DividerBrush = new(ThemePaletteProvider.Divider);
    private static readonly SolidColorBrush _ContrastedSurfaceBrush = new(ThemePaletteProvider.ContrastedSurface);
    private static readonly SolidColorBrush _NeutralSurfaceBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _HoverHighlightBrush = new(ThemePaletteProvider.HoverHighlight);

    private readonly KeyboardMapping _cloneSource;
    private readonly string? _existingLayoutName;
    private readonly PianoView _pianoView;
    private readonly IPianoEngine _pianoEngine;
    private readonly Dictionary<NotePitch, PianoKeyViewModel> _keysByPitch;
    private readonly TextBlock _statusText;
    private readonly NoteNameStyle _noteNameStyle;

    private NotePitch? _currentTarget;
    private int _targetIndex;

    public KeybindsEditorWindow(
        KeyboardMapping cloneSource,
        string? existingLayoutName,
        IPianoEngine pianoEngine,
        IOptionsMonitor<UIConfig> uiConfig,
        IOptionsMonitor<PianoConfig> pianoConfig)
    {
        _cloneSource = cloneSource;
        _existingLayoutName = existingLayoutName;
        _pianoEngine = pianoEngine;
        _noteNameStyle = uiConfig.CurrentValue.NoteNameStyle;

        List<PianoKeyViewModel> keys = PianoKeysBuilder.Build(
            pianoEngine,
            cloneSource.ToLabelsByPitch(),
            _noteNameStyle,
            new Transpose(pianoConfig.CurrentValue.Transpose),
            showKeyLabels: false,
            showNoteLabels: true);

        _keysByPitch = keys.ToDictionary(k => k.Pitch);
        _pianoView = new PianoView(keys)
        {
            Height = 120,
            IsHitTestVisible = false,
        };
        _statusText = BuildStatusStrip();

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);

        Content = BuildLayout();
        Closed += (_, _) => SetTarget(null);

        NavigateTo(0);
    }

    private void NavigateTo(int newIndex)
    {
        int maxIndex = PianoKeysBuilder.LastPitch - PianoKeysBuilder.FirstPitch;
        if (newIndex < 0 || newIndex > maxIndex)
        {
            return;
        }

        _targetIndex = newIndex;
        NotePitch target = new((ushort)(PianoKeysBuilder.FirstPitch + _targetIndex));
        SetTarget(target);
        _statusText.Text = $"Bind {NoteNames.GetNoteName(target, _noteNameStyle)}";
    }

    public void SetTarget(NotePitch? newTarget)
    {
        if (_currentTarget is { } prev)
        {
            if (_keysByPitch.TryGetValue(prev, out PianoKeyViewModel? prevKey))
            {
                prevKey.IsPressed = false;
            }
            _pianoEngine.NoteOff(prev);
        }

        _currentTarget = newTarget;

        if (newTarget is { } next)
        {
            if (_keysByPitch.TryGetValue(next, out PianoKeyViewModel? nextKey))
            {
                nextKey.IsPressed = true;
            }
            _pianoEngine.NoteOn(next);
        }
    }

    private Grid BuildLayout()
    {
        DockPanel header = BuildHeader();
        Control schema = BuildPlaceholder("PC keyboard schema (2.5)", minHeight: 120);
        StackPanel buttons = BuildButtonsRow();

        Grid root = new()
        {
            Margin = new Thickness(16),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
            },
        };

        Grid.SetRow(header, 0);
        Grid.SetRow(_pianoView, 1);
        Grid.SetRow(_statusText, 2);
        Grid.SetRow(schema, 3);
        Grid.SetRow(buttons, 4);

        root.Children.Add(header);
        root.Children.Add(_pianoView);
        root.Children.Add(_statusText);
        root.Children.Add(schema);
        root.Children.Add(buttons);

        return root;
    }

    private DockPanel BuildHeader()
    {
        TextBlock label = new()
        {
            Text = _ModifierLabel,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = _TextBrush,
            Margin = new Thickness(0, 0, 8, 0),
        };

        ComboBox combo = new()
        {
            ItemsSource = _ModifierOptions,
            SelectedItem = ModifierToOption(_cloneSource.BlackKeyModifier),
            MinWidth = 120,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
            Background = _ContrastedSurfaceBrush,
            BorderBrush = _NeutralSurfaceBrush,
        };
        combo.Resources["ComboBoxBorderBrushPointerOver"] = _HoverHighlightBrush;

        DockPanel.SetDock(label, Dock.Left);
        return new DockPanel
        {
            Margin = new Thickness(0, 0, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { label, combo },
        };
    }

    private static Border BuildPlaceholder(string text, double minHeight)
    {
        return new Border
        {
            BorderBrush = _DividerBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            MinHeight = minHeight,
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = _TextBrush,
            },
        };
    }

    private static TextBlock BuildStatusStrip()
    {
        return new TextBlock
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = _StatusFontSize,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = _TextBrush,
        };
    }

    private StackPanel BuildButtonsRow()
    {
        ToolbarButton backButton = new(_BackButtonLabel);
        backButton.PointerPressed += (_, e) =>
        {
            NavigateTo(_targetIndex - 1);
            e.Handled = true;
        };

        ToolbarButton skipButton = new(_SkipButtonLabel);
        skipButton.PointerPressed += (_, e) =>
        {
            NavigateTo(_targetIndex + 1);
            e.Handled = true;
        };

        ToolbarButton saveButton = new(_SaveButtonLabel);

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { backButton, skipButton, saveButton },
        };
    }

    private static string ModifierToOption(KeyModifiers modifier) => modifier switch
    {
        KeyModifiers.Shift => "Shift",
        KeyModifiers.Control => "Ctrl",
        KeyModifiers.Alt => "Alt",
        _ => "Shift",
    };
}
