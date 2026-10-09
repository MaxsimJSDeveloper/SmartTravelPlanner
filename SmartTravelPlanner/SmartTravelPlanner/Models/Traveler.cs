using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTravelPlanner.Exceptions;
using System.Text.Encodings.Web;

namespace SmartTravelPlanner.Models;

public sealed class Traveler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly List<string> _route = [];

    [JsonConstructor]
    public Traveler(string name, string currentLocation, IReadOnlyList<string>? route = null, int totalDistance = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(currentLocation))
        {
            throw new ArgumentException("Location cannot be empty.", nameof(currentLocation));
        }

        Name = name.Trim();
        CurrentLocation = currentLocation.Trim();

        if (route is { Count: > 0 })
        {
            if (!string.Equals(route[0]?.Trim(), CurrentLocation, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Route must start at the current location.", nameof(route));
            }

            UpdateRoute(route, totalDistance);
        }
    }

    public string Name { get; }

    public string CurrentLocation { get; }

    public IReadOnlyList<string> Route => _route.AsReadOnly();

    public int TotalDistance { get; private set; }

    public RouteResult PlanRoute(string? destination, CityGraph map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (string.IsNullOrWhiteSpace(destination))
        {
            return RouteResult.Fail("Destination cannot be empty.");
        }

        string target = destination.Trim();

        if (string.Equals(target, CurrentLocation, StringComparison.OrdinalIgnoreCase))
        {
            return RouteResult.Fail("Destination must differ from the current location.");
        }

        if (!map.ContainsCity(CurrentLocation))
        {
            return RouteResult.Fail($"Current location '{CurrentLocation}' is not on the map.");
        }

        if (!map.ContainsCity(target))
        {
            return RouteResult.Fail($"Destination '{target}' is not on the map.");
        }

        List<string> path = map.FindShortestPath(CurrentLocation, target);
        if (path.Count == 0)
        {
            return RouteResult.Fail($"Destination '{target}' is not reachable from '{CurrentLocation}'.");
        }

        int distance = map.GetPathDistance(path);
        UpdateRoute(path, distance);

        return RouteResult.Success(path, distance);
    }

    public void UpdateRoute(IEnumerable<string> path, int totalDistance)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (totalDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalDistance), totalDistance, "Distance cannot be negative.");
        }

        var cities = new List<string>();
        foreach (string city in path)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException("Route cannot contain empty city names.", nameof(path));
            }

            cities.Add(city.Trim());
        }

        _route.Clear();
        _route.AddRange(cities);
        TotalDistance = cities.Count == 0 ? 0 : totalDistance;
    }

    public void ClearRoute()
    {
        _route.Clear();
        TotalDistance = 0;
    }

    public void SaveToFile(string filePath)
    {
        try
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new TravelerFileException("Traveler file cannot be saved.", ex);
        }
    }

    public static Traveler LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new TravelerFileException("Traveler file not found.");
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Traveler>(json, JsonOptions)
                ?? throw new TravelerFileException("Traveler file is empty or has an invalid format.");
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new TravelerFileException("Traveler file is empty or has an invalid format.", ex);
        }
    }
}
