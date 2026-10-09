using System;
using System.Globalization;

namespace SmartTravelPlanner.Services;

public static class InputValidator
{
    public static string? ValidateRequired(string? value, string fieldName) =>
        string.IsNullOrWhiteSpace(value) ? $"{fieldName} cannot be empty." : null;

    public static string? ValidateName(string? name) => ValidateRequired(name, "Name");

    public static string? ValidateDestination(string? destination, string? currentLocation)
    {
        string? error = ValidateRequired(destination, "Destination");
        if (error is not null)
        {
            return error;
        }

        bool sameCity = string.Equals(
            destination!.Trim(),
            currentLocation?.Trim(),
            StringComparison.OrdinalIgnoreCase);

        return sameCity ? "Destination must differ from the current location." : null;
    }

    public static string? ValidateDistance(string? text, out int distance)
    {
        if (!int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out distance) || distance <= 0)
        {
            distance = 0;
            return "Distance must be a whole number greater than zero.";
        }

        return null;
    }
}