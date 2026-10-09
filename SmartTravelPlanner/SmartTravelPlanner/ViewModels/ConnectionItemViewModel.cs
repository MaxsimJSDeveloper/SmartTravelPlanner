using System;
using SmartTravelPlanner.Models;

namespace SmartTravelPlanner.ViewModels;

/// <summary>
/// One row (with a checkbox) in the "Existing connections" list of the map editor.
/// </summary>
public class ConnectionItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public ConnectionItemViewModel(Edge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        Edge = edge;
    }

    public Edge Edge { get; }

    public string DisplayText => Edge.DisplayText;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
