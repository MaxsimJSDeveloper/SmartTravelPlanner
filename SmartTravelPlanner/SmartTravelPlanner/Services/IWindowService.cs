using System.Threading.Tasks;
using SmartTravelPlanner.Models;

namespace SmartTravelPlanner.Services;

/// <summary>
/// Everything the view-models need from the UI layer (dialogs, file pickers, closing the app).
/// Keeping it behind an interface lets the view-models stay free of Avalonia types.
/// </summary>
public interface IWindowService
{
    Task ShowErrorAsync(string message);

    Task<string?> PickMapFileAsync();

    Task<string?> PickTravelerFileToOpenAsync();

    Task<string?> PickTravelerFileToSaveAsync(string suggestedFileName);

    /// <summary>
    /// Shows the map editor for the given working copy.
    /// </summary>
    /// <returns>The edited map if the user chose "Save &amp; Apply"; otherwise <see langword="null"/>.</returns>
    Task<CityGraph?> ShowMapEditorAsync(CityGraph workingCopy);

    void CloseApplication();
}
