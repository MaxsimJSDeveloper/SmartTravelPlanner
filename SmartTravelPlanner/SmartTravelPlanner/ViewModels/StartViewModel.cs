using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SmartTravelPlanner.Exceptions;
using SmartTravelPlanner.Models;
using SmartTravelPlanner.Services;

namespace SmartTravelPlanner.ViewModels;

/// <summary>
/// "Hello!" screen: creates a new traveler or loads a saved one.
/// </summary>
public class StartViewModel : ViewModelBase
{
    private readonly IWindowService _windows;
    private readonly Action<Traveler> _travelerReady;

    public StartViewModel(IWindowService windows, Action<Traveler> travelerReady)
    {
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(travelerReady);

        _windows = windows;
        _travelerReady = travelerReady;

        CreateTravelerCommand = new AsyncRelayCommand(CreateTravelerAsync);
        LoadSavedTravelerCommand = new AsyncRelayCommand(LoadSavedTravelerAsync);
    }

    public IAsyncRelayCommand CreateTravelerCommand { get; }

    public IAsyncRelayCommand LoadSavedTravelerCommand { get; }

    public FieldViewModel NameField { get; } = new();

    public FieldViewModel LocationField { get; } = new();

    private async Task CreateTravelerAsync()
    {
        bool nameValid = NameField.Validate(FormValidator.ValidateName);
        bool locationValid = LocationField.Validate(FormValidator.ValidateLocation);

        if (!nameValid || !locationValid)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(NameField.Text) && NameField.Error is not null)
            {
                messages.Add(NameField.Error);
            }

            if (string.IsNullOrWhiteSpace(LocationField.Text) && LocationField.Error is not null)
            {
                messages.Add(LocationField.Error);
            }

            if (messages.Count > 0)
            {
                await _windows.ShowErrorAsync(string.Join(Environment.NewLine, messages));
            }

            return;
        }

        _travelerReady(new Traveler(NameField.Text, LocationField.Text));
    }

    private async Task LoadSavedTravelerAsync()
    {
        string? path = await _windows.PickTravelerFileToOpenAsync();
        if (path is null)
        {
            return;
        }

        Traveler traveler;
        try
        {
            traveler = Traveler.LoadFromFile(path);
        }
        catch (TravelerFileException ex)
        {
            await _windows.ShowErrorAsync(ex.Message);
            return;
        }

        _travelerReady(traveler);
    }
}