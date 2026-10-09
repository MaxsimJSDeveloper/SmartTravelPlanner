using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SmartTravelPlanner.Models;
using SmartTravelPlanner.Services;

namespace SmartTravelPlanner.ViewModels;

/// <summary>
/// Map editor: add and remove connections between cities in a working copy of the map.
/// </summary>
public class MapEditorViewModel : ViewModelBase
{
    private readonly CityGraph _graph;
    private readonly IWindowService _windows;
    private readonly Action<bool> _close;

    /// <param name="workingCopy">Copy of the map that the editor changes.</param>
    /// <param name="windows">Used to show error dialogs.</param>
    /// <param name="close">Closes the editor window; <see langword="true"/> means "Save &amp; Apply".</param>
    public MapEditorViewModel(CityGraph workingCopy, IWindowService windows, Action<bool> close)
    {
        ArgumentNullException.ThrowIfNull(workingCopy);
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(close);

        _graph = workingCopy;
        _windows = windows;
        _close = close;

        AddConnectionCommand = new RelayCommand(AddConnection);
        RemoveSelectedCommand = new AsyncRelayCommand(RemoveSelectedAsync);
        SaveAndApplyCommand = new AsyncRelayCommand(SaveAndApplyAsync);
        CancelCommand = new RelayCommand(() => _close(false));

        ReloadConnections();
    }

    public ObservableCollection<ConnectionItemViewModel> Connections { get; } = [];

    public IRelayCommand AddConnectionCommand { get; }

    public IAsyncRelayCommand RemoveSelectedCommand { get; }

    public IAsyncRelayCommand SaveAndApplyCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public FieldViewModel City1Field { get; } = new();

    public FieldViewModel City2Field { get; } = new();

    public FieldViewModel DistanceField { get; } = new();

    private void AddConnection()
    {
        bool city1Valid = City1Field.Validate(text => FormValidator.ValidateCity(text, "City 1"));
        bool city2Valid = City2Field.Validate(text => FormValidator.ValidateCity(text, "City 2"));
        bool distanceValid = DistanceField.Validate(FormValidator.ValidateDistance);

        if (city1Valid && city2Valid &&
            string.Equals(City1Field.Text.Trim(), City2Field.Text.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            City2Field.SetError("Cities must be different.");
            city2Valid = false;
        }

        if (!city1Valid || !city2Valid || !distanceValid)
        {
            return;
        }

        InputValidator.ValidateDistance(DistanceField.Text, out int distance);
        _graph.AddEdge(City1Field.Text, City2Field.Text, distance);
        ReloadConnections();

        City1Field.Text = string.Empty;
        City2Field.Text = string.Empty;
        DistanceField.Text = string.Empty;
    }

    private async Task RemoveSelectedAsync()
    {
        var selected = Connections.Where(item => item.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await _windows.ShowErrorAsync("Select at least one connection to remove.");
            return;
        }

        foreach (ConnectionItemViewModel item in selected)
        {
            _graph.RemoveEdge(item.Edge.City1, item.Edge.City2);
        }

        ReloadConnections();
    }

    private async Task SaveAndApplyAsync()
    {
        if (Connections.Count == 0)
        {
            await _windows.ShowErrorAsync("The map must contain at least one connection.");
            return;
        }

        _close(true);
    }

    private void ReloadConnections()
    {
        Connections.Clear();

        foreach (Edge edge in _graph.GetAllEdges())
        {
            Connections.Add(new ConnectionItemViewModel(edge));
        }
    }
}
