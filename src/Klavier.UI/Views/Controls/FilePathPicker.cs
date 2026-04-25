using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Klavier.UI.Theme;

namespace Klavier.UI.Views.Controls;

public class FilePathPicker : Border
{
    private const double _DefaultMinWidth = 200;

    private static readonly SolidColorBrush _ContrastedSurfaceBrush = new(ThemePaletteProvider.ContrastedSurface);
    private static readonly SolidColorBrush _NeutralSurfaceBrush = new(ThemePaletteProvider.NeutralSurface);
    private static readonly SolidColorBrush _TextBrush = new(ThemePaletteProvider.TextPrimary);

    // Folder glyph (Material Icons "folder", 24x24 viewport).
    private static readonly Geometry _FolderGeometry = Geometry.Parse(
        "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z");

    private readonly TextBox _pathDisplay;
    private readonly IconButton _pickerButton;
    private readonly string _pickerTitle;
    private readonly FilePickerFileType _fileType;
    private readonly Func<string?> _getCurrentPath;
    private readonly Func<string?>? _getPrettyName;
    private readonly Func<string, Task<bool>> _onPickAsync;

    public FilePathPicker(
        string pickerTitle,
        string folderTooltip,
        FilePickerFileType fileType,
        Func<string?> getCurrentPath,
        Func<string?>? getPrettyName,
        Func<string, Task<bool>> onPickAsync)
    {
        _pickerTitle = pickerTitle;
        _fileType = fileType;
        _getCurrentPath = getCurrentPath;
        _getPrettyName = getPrettyName;
        _onPickAsync = onPickAsync;

        _pathDisplay = new TextBox
        {
            IsReadOnly = true,
            Focusable = false,
            Foreground = _TextBrush,
            FontSize = Constants.PrimaryFontSize,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 0),
        };

        _pickerButton = new IconButton(_FolderGeometry)
        {
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        ToolTip.SetTip(_pickerButton, folderTooltip);
        DockPanel.SetDock(_pickerButton, Dock.Right);

        _pickerButton.PointerPressed += async (_, e) =>
        {
            e.Handled = true;
            try
            {
                await HandlePick();
            }
            finally
            {
                _pickerButton.IsActive = false;
            }
        };

        Background = _ContrastedSurfaceBrush;
        BorderBrush = _NeutralSurfaceBrush;
        BorderThickness = new Thickness(Constants.BorderThickness);
        CornerRadius = new CornerRadius(Constants.CornerRadius);
        ClipToBounds = true;
        MinWidth = _DefaultMinWidth;
        HorizontalAlignment = HorizontalAlignment.Left;
        Child = new DockPanel
        {
            LastChildFill = true,
            Children = { _pickerButton, _pathDisplay },
        };

        Refresh();
    }

    public void Refresh()
    {
        UpdateDisplayAndTooltip(_getCurrentPath() ?? string.Empty);
    }

    private void UpdateDisplayAndTooltip(string path)
    {
        string? prettyName = _getPrettyName?.Invoke();
        _pathDisplay.Text = ComputeDisplayName(prettyName, path);
        if (!string.IsNullOrEmpty(path))
        {
            ToolTip.SetTip(_pathDisplay, path);
        }
    }

    private static string ComputeDisplayName(string? prettyName, string filePath)
    {
        return !string.IsNullOrWhiteSpace(prettyName) ? prettyName : Path.GetFileName(filePath);
    }

    private async Task HandlePick()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        IStorageFolder? suggestedFolder = null;
        string? currentDir = Path.GetDirectoryName(_getCurrentPath());
        if (!string.IsNullOrEmpty(currentDir))
        {
            suggestedFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(currentDir);
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _pickerTitle,
            AllowMultiple = false,
            FileTypeFilter = [_fileType],
            SuggestedStartLocation = suggestedFolder,
        });
        if (files.Count == 0)
        {
            return;
        }

        string newPath = files[0].Path.LocalPath;
        if (await _onPickAsync(newPath))
        {
            UpdateDisplayAndTooltip(newPath);
        }
    }
}
