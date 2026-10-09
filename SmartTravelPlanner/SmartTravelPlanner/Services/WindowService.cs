using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SmartTravelPlanner.Models;
using SmartTravelPlanner.ViewModels;
using SmartTravelPlanner.Views;

namespace SmartTravelPlanner.Services;

/// <summary>
/// Avalonia implementation of <see cref="IWindowService"/>: error dialogs, file pickers and the map editor window.
/// </summary>
public sealed class WindowService : IWindowService
{
    private static readonly FilePickerFileType MapFiles = new("Map files") { Patterns = ["*.txt"] };
    private static readonly FilePickerFileType TravelerFiles = new("Traveler files") { Patterns = ["*.json"] };

    private readonly Window _mainWindow;
    private Window _activeWindow;

    public WindowService(Window mainWindow)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);

        _mainWindow = mainWindow;
        _activeWindow = mainWindow;
    }

    public async Task ShowErrorAsync(string message)
    {
        var dialog = new ErrorDialogWindow(message);
        await dialog.ShowDialog(_activeWindow);
    }

    public Task<string?> PickMapFileAsync() =>
        PickFileToOpenAsync("Load map", MapFiles);

    public Task<string?> PickTravelerFileToOpenAsync() =>
        PickFileToOpenAsync("Load traveler", TravelerFiles);

    public async Task<string?> PickTravelerFileToSaveAsync(string suggestedFileName)
    {
        using IStorageFile? file = await _activeWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save traveler",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "json",
            FileTypeChoices = [TravelerFiles],
        });

        return file?.TryGetLocalPath();
    }

    public async Task<CityGraph?> ShowMapEditorAsync(CityGraph workingCopy)
    {
        MapEditorWindow? editor = null;
        var viewModel = new MapEditorViewModel(workingCopy, this, isApplied => editor?.Close(isApplied));
        editor = new MapEditorWindow { DataContext = viewModel };

        Window previous = _activeWindow;
        _activeWindow = editor;

        try
        {
            bool applied = await editor.ShowDialog<bool>(previous);
            return applied ? workingCopy : null;
        }
        finally
        {
            _activeWindow = previous;
        }
    }

    public void CloseApplication() => _mainWindow.Close();

    private async Task<string?> PickFileToOpenAsync(string title, FilePickerFileType fileType)
    {
        IReadOnlyList<IStorageFile> files = await _activeWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [fileType],
        });

        try
        {
            return files.Count == 0 ? null : files[0].TryGetLocalPath();
        }
        finally
        {
            foreach (IStorageFile file in files)
            {
                file.Dispose();
            }
        }
    }
}
