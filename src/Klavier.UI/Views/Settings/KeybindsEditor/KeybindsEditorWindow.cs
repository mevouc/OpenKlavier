using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Klavier.Config;
using Klavier.Core.Engine;
using Klavier.Core.Music;
using Klavier.Core.Primitives;
using Klavier.UI.Input.Mapping;
using Klavier.UI.Theme;
using Klavier.UI.ViewModels;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Piano;
using Microsoft.Extensions.Options;

namespace Klavier.UI.Views.Settings.KeybindsEditor;

public class KeybindsEditorWindow : Window
{
    private const string _WindowTitle = "Keybinds Editor";
    private const int _DefaultWidth = 800;
    private const int _DefaultHeight = 500;
    private const int _MinWidth = 720;
    private const int _MinHeight = 450;
    private const string _ModifierLabel = "Modifier for black keys:";
    private const string _BackButtonLabel = "Back";
    private const string _SkipButtonLabel = "Skip";
    private const string _SaveButtonLabel = "Save";
    private const double _StatusFontSize = 20;
    private const double _PianoFixedHeight = 120;
    private const double _RootMargin = 16;
    private const double _HeaderBottomMargin = 12;
    private const double _StatusBottomMargin = 8;
    private const double _ModifierLabelRightMargin = 8;
    private const double _ButtonsRowSpacing = 8;

    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);

    private readonly KeyboardMapping _cloneSource;
    private readonly string? _existingLayoutName;
    private readonly PianoView _pianoView;
    private readonly IPianoEngine _pianoEngine;
    private readonly Dictionary<NotePitch, PianoKeyViewModel> _keysByPitch;
    private readonly TextBlock _statusText;
    private readonly KlavierComboBox _modifierCombo;
    private readonly NoteNameStyle _noteNameStyle;

    private int _targetIndex;
    private bool _hasActiveTarget;

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
            showKeyLabels: true,
            showNoteLabels: true);

        _keysByPitch = keys.ToDictionary(k => k.Pitch);
        _pianoView = new PianoView(keys)
        {
            Height = _PianoFixedHeight,
            IsHitTestVisible = false,
        };
        _statusText = BuildStatusStrip();
        _modifierCombo = BuildModifierCombo();
        // TODO (2.7): wire _modifierCombo.SelectionChanged to confirm dialog + schema refresh.

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);

        Content = BuildLayout();
        Closed += (_, _) => Release();

        NavigateTo(0);
    }

    private void NavigateTo(int newIndex)
    {
        const int maxIndex = PianoKeysBuilder.LastPitch - PianoKeysBuilder.FirstPitch;
        if (newIndex < 0 || newIndex > maxIndex)
        {
            return;
        }

        Release();
        _targetIndex = newIndex;
        Press();
    }

    private NotePitch CurrentPitch => new((ushort)(PianoKeysBuilder.FirstPitch + _targetIndex));

    private void Press()
    {
        NotePitch pitch = CurrentPitch;
        if (_keysByPitch.TryGetValue(pitch, out PianoKeyViewModel? key))
        {
            key.IsPressed = true;
        }
        _pianoEngine.NoteOn(pitch);
        _statusText.Text = $"Bind {NoteNames.GetNoteName(pitch, _noteNameStyle)}";
        _hasActiveTarget = true;
    }

    private void Release()
    {
        if (!_hasActiveTarget)
        {
            return;
        }
        NotePitch pitch = CurrentPitch;
        if (_keysByPitch.TryGetValue(pitch, out PianoKeyViewModel? key))
        {
            key.IsPressed = false;
        }
        _pianoEngine.NoteOff(pitch);
        _hasActiveTarget = false;
    }

    private Grid BuildLayout()
    {
        DockPanel header = BuildHeader();
        Viewbox schema = new()
        {
            Stretch = Stretch.Uniform,
            Child = new PcKeyboardSchema(
                _cloneSource.WhiteKeys,
                _cloneSource.BlackKeys,
                _cloneSource.BlackKeyModifier,
                _noteNameStyle),
        };
        StackPanel buttons = BuildButtonsRow();

        Grid root = new()
        {
            Margin = new Thickness(_RootMargin),
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
            Margin = new Thickness(0, 0, _ModifierLabelRightMargin, 0),
        };

        DockPanel.SetDock(label, Dock.Left);
        return new DockPanel
        {
            Margin = new Thickness(0, 0, 0, _HeaderBottomMargin),
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { label, _modifierCombo },
        };
    }

    private KlavierComboBox BuildModifierCombo() => new()
    {
        ItemsSource = KeyModifierOptions.AllLabels,
        SelectedItem = KeyModifierOptions.LabelOf(_cloneSource.BlackKeyModifier),
    };

    private static TextBlock BuildStatusStrip()
    {
        return new TextBlock
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = _StatusFontSize,
            Margin = new Thickness(0, 0, 0, _StatusBottomMargin),
            Foreground = _TextBrush,
        };
    }

    private StackPanel BuildButtonsRow()
    {
        KlavierButton backButton = new(_BackButtonLabel);
        backButton.PointerPressed += (_, e) =>
        {
            NavigateTo(_targetIndex - 1);
            e.Handled = true;
        };

        KlavierButton skipButton = new(_SkipButtonLabel);
        skipButton.PointerPressed += (_, e) =>
        {
            NavigateTo(_targetIndex + 1);
            e.Handled = true;
        };

        KlavierButton saveButton = new(_SaveButtonLabel);

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = _ButtonsRowSpacing,
            Children = { backButton, skipButton, saveButton },
        };
    }
}
