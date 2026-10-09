using System;
using SmartTravelPlanner.Models;
using SmartTravelPlanner.Services;

namespace SmartTravelPlanner.ViewModels;

/// <summary>
/// Shell of the application: decides which page (start screen or planner) is shown in the main window.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly IWindowService _windows;
    private ViewModelBase _currentPage;

    public MainViewModel(IWindowService windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        _windows = windows;
        _currentPage = new StartViewModel(windows, ShowPlanner);
    }

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    private void ShowPlanner(Traveler traveler) =>
        CurrentPage = new PlannerViewModel(traveler, _windows);
}
