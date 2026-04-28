using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Klavier.UI.Theme;
using Klavier.UI.Views.Controls;
using Klavier.UI.Views.Piano;
using Klavier.UI.Views.Player;
using Klavier.UI.Views.Settings;

namespace Klavier.UI.Views.Layout;

/// <summary>
/// Builds and owns MainWindow's layout tree: outer 4-row grid (player / piano section / splitter / settings),
/// the piano sub-section (piano / separator / toolbar), the splitters, and both <see cref="CollapsibleSection"/>s.
/// Exposes <see cref="Root"/> for the window's <c>Content</c> and the two collapsible sections so the orchestrator
/// can wire toolbar toggles to them.
/// </summary>
public class MainWindowLayout
{
    private const int _SplitterHeight = 8;
    private const int _DefaultPlayerHeight = 250;
    private const int _DefaultSettingsHeight = 300;
    private const int _PlayerMinHeight = 80;
    private const int _SettingsMinHeight = 150;
    private const int _PianoMinHeight = 100;
    private const int _PianoSectionMinHeight = 150;

    public Panel Root { get; }
    public CollapsibleSection PlayerSection { get; }
    public CollapsibleSection SettingsSection { get; }

    public MainWindowLayout(
        Window window,
        PianoView pianoView,
        PlayerView playerView,
        ToolbarView toolbarView,
        SettingsView settingsPanel,
        DropOverlay dropOverlay)
    {
        Grid pianoSection = BuildPianoSection(pianoView, toolbarView);

        // Player splitter straddles the player row's bottom edge - zero layout space, half-overflows into the piano section.
        DraggableSplitter playerSplitter = new(_SplitterHeight);
        playerSplitter.StraddleBottomBoundary();
        // Settings splitter sits in its own row, taking _SplitterHeight of layout space.
        DraggableSplitter settingsSplitter = new(_SplitterHeight);

        // Outer layout: player (collapsible, contains its straddling splitter) / pianoSection / settingsSplitter / settings (collapsible).
        RowDefinition playerRow = new();
        RowDefinition pianoSectionRow = new() { Height = new GridLength(1, GridUnitType.Star), MinHeight = _PianoSectionMinHeight };
        RowDefinition settingsSplitterRow = new() { Height = GridLength.Auto };
        RowDefinition settingsRow = new();

        Grid.SetRow(playerView, 0);
        Grid.SetRow(playerSplitter.HitArea, 0);
        Grid.SetRow(playerSplitter.Visual, 0);
        Grid.SetRow(pianoSection, 1);
        Grid.SetRow(settingsSplitter.HitArea, 2);
        Grid.SetRow(settingsSplitter.Visual, 2);
        Grid.SetRow(settingsPanel, 3);

        Grid mainGrid = new()
        {
            RowDefinitions = { playerRow, pianoSectionRow, settingsSplitterRow, settingsRow },
            Children =
            {
                playerView,
                playerSplitter.HitArea, playerSplitter.Visual,
                pianoSection,
                settingsSplitter.HitArea, settingsSplitter.Visual,
                settingsPanel,
            },
        };

        Root = new Panel { Children = { mainGrid, dropOverlay } };

        PlayerSection = new CollapsibleSection(
            content: playerView,
            splitter: playerSplitter,
            row: playerRow,
            window: window,
            defaultHeight: _DefaultPlayerHeight,
            minHeight: _PlayerMinHeight,
            splitterLayoutHeight: 0,
            growUpward: true);

        SettingsSection = new CollapsibleSection(
            content: settingsPanel,
            splitter: settingsSplitter,
            row: settingsRow,
            window: window,
            defaultHeight: _DefaultSettingsHeight,
            minHeight: _SettingsMinHeight,
            splitterLayoutHeight: _SplitterHeight,
            growUpward: false,
            measureContentHeight: () =>
            {
                settingsPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return settingsPanel.DesiredSize.Height;
            });
    }

    // Piano section: piano (star) / separator / toolbar.
    private static Grid BuildPianoSection(PianoView pianoView, ToolbarView toolbarView)
    {
        Grid separator = CreatePianoSeparator();
        Grid pianoSection = new()
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = _PianoMinHeight },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
            },
        };
        Grid.SetRow(pianoView, 0);
        Grid.SetRow(separator, 1);
        Grid.SetRow(toolbarView, 2);
        pianoSection.Children.Add(pianoView);
        pianoSection.Children.Add(separator);
        pianoSection.Children.Add(toolbarView);
        return pianoSection;
    }

    private static Grid CreatePianoSeparator()
    {
        Border line = new()
        {
            Height = 1,
            Background = new SolidColorBrush(ThemePaletteProvider.Divider),
        };

        Grid grid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            },
        };

        Grid.SetColumn(line, 1);
        grid.Children.Add(line);

        return grid;
    }
}
