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
    private const string _CancelButtonLabel = "Cancel";
    private const string _SaveButtonLabel = "Save";
    private const string _IdleStatusText = "Click a piano key to remap.";
    private const double _StatusFontSize = 20;
    private const double _PianoFixedHeight = 120;
    private const double _RootMargin = 16;
    private const double _HeaderBottomMargin = 12;
    private const double _StatusBottomMargin = 8;
    private const double _ModifierLabelRightMargin = 8;
    private const double _ButtonsRowSpacing = 8;

    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);

    private readonly string? _existingLayoutName;
    private readonly KeybindsEditSession _session;
    private readonly PianoView _pianoView;
    private readonly IPianoEngine _pianoEngine;
    private readonly Dictionary<NotePitch, PianoKeyViewModel> _keysByPitch;
    private readonly TextBlock _statusText;
    private readonly KlavierComboBox _modifierCombo;
    private readonly Viewbox _schemaViewbox;
    private readonly NoteNameStyle _noteNameStyle;

    private NotePitch? _pendingTarget;

    public KeybindsEditorWindow(
        KeyboardMapping cloneSource,
        string? existingLayoutName,
        IPianoEngine pianoEngine,
        IOptionsMonitor<UIConfig> uiConfig,
        IOptionsMonitor<PianoConfig> pianoConfig)
    {
        _existingLayoutName = existingLayoutName;
        _pianoEngine = pianoEngine;
        _noteNameStyle = uiConfig.CurrentValue.NoteNameStyle;
        _session = new KeybindsEditSession(cloneSource);

        List<PianoKeyViewModel> keys = PianoKeysBuilder.Build(
            pianoEngine,
            cloneSource.ToLabelsByPitch(),
            _noteNameStyle,
            new Transpose(pianoConfig.CurrentValue.Transpose),
            showKeyLabels: true,
            showNoteLabels: true);

        _keysByPitch = keys.ToDictionary(k => k.Pitch);
        _pianoView = new PianoView(keys) { Height = _PianoFixedHeight };
        WirePianoInteractivity();

        _statusText = BuildStatusStrip();
        _modifierCombo = BuildModifierCombo();
        // TODO (2.7): wire _modifierCombo.SelectionChanged to confirm dialog + schema refresh.

        _schemaViewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            Child = BuildSchema(),
        };
        _session.BindingsChanged += OnBindingsChanged;

        Title = _WindowTitle;
        Width = _DefaultWidth;
        Height = _DefaultHeight;
        MinWidth = _MinWidth;
        MinHeight = _MinHeight;
        Background = new SolidColorBrush(ThemePaletteProvider.AppBackground);

        Content = BuildLayout();
        _statusText.Text = _IdleStatusText;
        Closed += (_, _) => ClearPendingTarget();
    }

    private void WirePianoInteractivity()
    {
        foreach (PianoKeyControl whiteKey in _pianoView.WhiteKeys)
        {
            whiteKey.InteractionMode = PianoKeyInteractionMode.Select;
            whiteKey.KeyClicked += (_, pitch) => SelectTarget(pitch);
        }
        foreach (PianoKeyControl blackKey in _pianoView.BlackKeys)
        {
            blackKey.IsHitTestVisible = false;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_pendingTarget is not { } targetPitch)
        {
            base.OnKeyDown(e);
            return;
        }

        if (e.Key == Key.Escape)
        {
            ClearPendingTarget();
            e.Handled = true;
            return;
        }

        if (!BindableKeys.All.Contains(e.PhysicalKey))
        {
            _statusText.Text = $"'{e.PhysicalKey}' is not a bindable key. Press a letter/digit/punctuation, Esc to cancel.";
            e.Handled = true;
            return;
        }

        BindingResult result = _session.Apply(targetPitch, e.PhysicalKey, e.KeySymbol);
        ClearPendingTarget();

        if (result.DisplacedFromPitch is { } displaced)
        {
            _statusText.Text = $"Moved binding away from {NoteNames.GetNoteName(displaced, _noteNameStyle)}.";
        }

        e.Handled = true;
    }

    private void SelectTarget(NotePitch pitch)
    {
        ClearPendingTarget();
        _pendingTarget = pitch;
        if (_keysByPitch.TryGetValue(pitch, out PianoKeyViewModel? vm))
        {
            vm.IsPressed = true;
        }
        _pianoEngine.NoteOn(pitch);
        _statusText.Text = $"Press a keyboard key to bind {NoteNames.GetNoteName(pitch, _noteNameStyle)}. Esc to cancel.";
    }

    private void ClearPendingTarget()
    {
        if (_pendingTarget is not { } pitch)
        {
            return;
        }
        if (_keysByPitch.TryGetValue(pitch, out PianoKeyViewModel? vm))
        {
            vm.IsPressed = false;
        }
        _pianoEngine.NoteOff(pitch);
        _pendingTarget = null;
        _statusText.Text = _IdleStatusText;
    }

    private PcKeyboardSchema BuildSchema() => new(
        _session.WhiteBindings,
        _session.BlackBindings,
        _session.BlackKeyModifier,
        _noteNameStyle);

    private void OnBindingsChanged()
    {
        _schemaViewbox.Child = BuildSchema();
        SyncPianoLabelsFromSession();
    }

    private void SyncPianoLabelsFromSession()
    {
        Dictionary<NotePitch, string> labels = [];
        foreach (KeyMappingEntry entry in _session.WhiteBindings.Values)
        {
            labels[entry.Pitch] = entry.Label;
        }
        foreach (KeyMappingEntry entry in _session.BlackBindings.Values)
        {
            labels[entry.Pitch] = entry.Label;
        }

        foreach (PianoKeyViewModel vm in _keysByPitch.Values)
        {
            vm.KeyLabel = labels.TryGetValue(vm.Pitch, out string? label) ? label : string.Empty;
        }
    }

    private Grid BuildLayout()
    {
        DockPanel header = BuildHeader();
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
        Grid.SetRow(_schemaViewbox, 3);
        Grid.SetRow(buttons, 4);

        root.Children.Add(header);
        root.Children.Add(_pianoView);
        root.Children.Add(_statusText);
        root.Children.Add(_schemaViewbox);
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
        SelectedItem = KeyModifierOptions.LabelOf(_session.BlackKeyModifier),
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
        KlavierButton cancelButton = new(_CancelButtonLabel);
        cancelButton.PointerPressed += (_, e) =>
        {
            Close();
            e.Handled = true;
        };

        KlavierButton saveButton = new(_SaveButtonLabel);
        // TODO (2.8): wire Save.

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = _ButtonsRowSpacing,
            Children = { cancelButton, saveButton },
        };
    }
}
