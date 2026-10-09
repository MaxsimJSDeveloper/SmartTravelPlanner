using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SmartTravelPlanner.Exceptions;
using SmartTravelPlanner.Models;
using SmartTravelPlanner.Services;

namespace SmartTravelPlanner.ViewModels;

public class PlannerViewModel : ViewModelBase
{
    private const string RouteSeparator = " → ";
    private const string NoMapText = "No map loaded.";

    private readonly IWindowService _windows;

    private Traveler _traveler;
    private CityGraph? _map;
    private string _mapStatus = NoMapText;
    private string _routeText = string.Empty;
    private string _totalDistanceText = string.Empty;

    public PlannerViewModel(Traveler traveler, IWindowService windows)
    {
        ArgumentNullException.ThrowIfNull(traveler);
        ArgumentNullException.ThrowIfNull(windows);

        _traveler = traveler;
        _windows = windows;

        LoadMapCommand = new AsyncRelayCommand(LoadMapAsync);
        EditMapCommand = new AsyncRelayCommand(EditMapAsync);
        ChangeLocationCommand = new AsyncRelayCommand(ChangeLocationAsync);
        PlanRouteCommand = new AsyncRelayCommand(PlanRouteAsync);
        ClearRouteCommand = new RelayCommand(ClearRoute);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        LoadCommand = new AsyncRelayCommand(LoadTravelerAsync);
        ExitCommand = new RelayCommand(_windows.CloseApplication);

        LocationField.Text = _traveler.CurrentLocation;
        ShowTravelerRoute();
        FillDestinationFromRoute();
    }

    public IAsyncRelayCommand LoadMapCommand { get; }

    public IAsyncRelayCommand EditMapCommand { get; }

    public IAsyncRelayCommand ChangeLocationCommand { get; }

    public IAsyncRelayCommand PlanRouteCommand { get; }

    public IRelayCommand ClearRouteCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand LoadCommand { get; }

    public IRelayCommand ExitCommand { get; }

    public string WelcomeText => $"Welcome {_traveler.Name}!";

    public FieldViewModel LocationField { get; } = new();

    public FieldViewModel DestinationField { get; } = new();

    public string MapStatus
    {
        get => _mapStatus;
        private set => SetProperty(ref _mapStatus, value);
    }

    public string RouteText
    {
        get => _routeText;
        private set => SetProperty(ref _routeText, value);
    }

    public string TotalDistanceText
    {
        get => _totalDistanceText;
        private set => SetProperty(ref _totalDistanceText, value);
    }

    private async Task LoadMapAsync()
    {
        string? path = await _windows.PickMapFileAsync();
        if (path is null)
        {
            return;
        }

        CityGraph map;
        try
        {
            map = CityGraph.LoadFromFile(path);
        }
        catch (MapFormatException ex)
        {
            await _windows.ShowErrorAsync(ex.Message);
            return;
        }

        ApplyMap(map, $"Map: {Path.GetFileName(path)}");
        await DropRouteIfNotOnMapAsync();
    }

    private async Task EditMapAsync()
    {
        CityGraph workingCopy = _map?.Clone() ?? new CityGraph();

        CityGraph? edited = await _windows.ShowMapEditorAsync(workingCopy);
        if (edited is null)
        {
            return;
        }

        ApplyMap(edited, "Map: edited manually");
        await DropRouteIfNotOnMapAsync();
    }

    private async Task PlanRouteAsync()
    {
        bool locationValid = TryApplyLocation();
        bool destinationValid = DestinationField.Validate(
            text => FormValidator.ValidateDestination(text, _traveler.CurrentLocation));

        if (!locationValid || !destinationValid)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(LocationField.Text) && LocationField.Error is not null)
            {
                messages.Add(LocationField.Error);
            }

            bool destinationEmpty = string.IsNullOrWhiteSpace(DestinationField.Text);
            bool sameCity = string.Equals(
                DestinationField.Text.Trim(), _traveler.CurrentLocation, StringComparison.OrdinalIgnoreCase);
            if ((destinationEmpty || sameCity) && DestinationField.Error is not null)
            {
                messages.Add(DestinationField.Error);
            }

            if (messages.Count > 0)
            {
                await _windows.ShowErrorAsync(string.Join(Environment.NewLine, messages));
            }

            return;
        }

        if (_map is null)
        {
            await _windows.ShowErrorAsync("Map is not loaded. Load a map file first.");
            return;
        }

        if (!_map.ContainsCity(_traveler.CurrentLocation))
        {
            await ShowFieldErrorAsync(
                LocationField, $"Current location '{_traveler.CurrentLocation}' is not on the map.");
            return;
        }

        RouteResult result = _traveler.PlanRoute(DestinationField.Text, _map);
        if (!result.IsSuccess)
        {
            await ShowFieldErrorAsync(DestinationField, result.ErrorMessage ?? "Route cannot be planned.");
            return;
        }

        ShowTravelerRoute();
    }

    private async Task ChangeLocationAsync() => await TryApplyLocationAsync();

    private async Task<bool> TryApplyLocationAsync()
    {
        if (TryApplyLocation())
        {
            return true;
        }

        if (LocationField.Error is not null)
        {
            await _windows.ShowErrorAsync(LocationField.Error);
        }

        return false;
    }

    private async Task ShowFieldErrorAsync(FieldViewModel field, string message)
    {
        field.SetError(message);
        await _windows.ShowErrorAsync(message);
    }

    private void ClearRoute()
    {
        _traveler.ClearRoute();
        DestinationField.Reset();
        ShowTravelerRoute();
    }

    private async Task SaveAsync()
    {
        if (!await TryApplyLocationAsync())
        {
            return;
        }

        string? path = await _windows.PickTravelerFileToSaveAsync($"{_traveler.Name}.json");
        if (path is null)
        {
            return;
        }

        try
        {
            _traveler.SaveToFile(path);
        }
        catch (TravelerFileException ex)
        {
            await _windows.ShowErrorAsync(ex.Message);
        }
    }

    private async Task LoadTravelerAsync()
    {
        string? path = await _windows.PickTravelerFileToOpenAsync();
        if (path is null)
        {
            return;
        }

        Traveler loaded;
        try
        {
            loaded = Traveler.LoadFromFile(path);
        }
        catch (TravelerFileException ex)
        {
            await _windows.ShowErrorAsync(ex.Message);
            return;
        }

        _traveler = loaded;
        DestinationField.Reset();
        LocationField.Reset();
        LocationField.Text = _traveler.CurrentLocation;
        OnPropertyChanged(nameof(WelcomeText));
        ShowTravelerRoute();
        await DropRouteIfNotOnMapAsync();
        FillDestinationFromRoute();
    }

    private void FillDestinationFromRoute()
    {
        if (_traveler.Route.Count > 1)
        {
            DestinationField.Text = _traveler.Route.LastOrDefault() ?? string.Empty;
        }
    }

    private bool TryApplyLocation()
    {
        if (!LocationField.Validate(FormValidator.ValidateLocation))
        {
            return false;
        }

        string location = LocationField.Text.Trim();
        LocationField.Text = location;

        if (string.Equals(location, _traveler.CurrentLocation, StringComparison.Ordinal))
        {
            return true;
        }

        _traveler = new Traveler(_traveler.Name, location);
        ShowTravelerRoute();
        return true;
    }

    private void ApplyMap(CityGraph map, string description)
    {
        _map = map;
        MapStatus = $"{description} ({map.GetAllEdges().Count} connections)";
    }

    private async Task DropRouteIfNotOnMapAsync()
    {
        if (_map is null || _traveler.Route.Count == 0)
        {
            return;
        }

        if (_map.GetPathDistance(_traveler.Route) == _traveler.TotalDistance)
        {
            return;
        }

        _traveler.ClearRoute();
        DestinationField.Reset();
        ShowTravelerRoute();
        await _windows.ShowErrorAsync("The planned route does not match the current map and was cleared.");
    }

    private void ShowTravelerRoute()
    {
        bool hasRoute = _traveler.Route.Count > 0;

        RouteText = hasRoute ? string.Join(RouteSeparator, _traveler.Route) : string.Empty;
        TotalDistanceText = hasRoute ? $"{_traveler.TotalDistance} km" : string.Empty;
    }
}