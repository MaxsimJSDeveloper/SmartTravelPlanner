using System.Linq;

namespace SmartTravelPlanner.Services;

/// <summary>
/// Stricter checks for text typed into the UI. Empty-value and "same city" checks are delegated to
/// <see cref="InputValidator"/>, so the backend rules stay in one place.
/// </summary>
public static class FormValidator
{
    public const int MaxTextLength = 50;
    public const int MaxDistance = 100_000;

    private const int MinLetters = 2;

    public static string? ValidateName(string? name) =>
        InputValidator.ValidateName(name) ?? ValidateText(name!, "Name");

    public static string? ValidateLocation(string? location) => ValidateCity(location, "Location");

    public static string? ValidateCity(string? city, string fieldName) =>
        InputValidator.ValidateRequired(city, fieldName) ?? ValidateText(city!, fieldName);

    public static string? ValidateDestination(string? destination, string? currentLocation) =>
        InputValidator.ValidateRequired(destination, "Destination")
        ?? ValidateText(destination!, "Destination")
        ?? InputValidator.ValidateDestination(destination, currentLocation);

    public static string? ValidateDistance(string? text)
    {
        string? error = InputValidator.ValidateDistance(text, out int distance);
        if (error is not null)
        {
            return error;
        }

        return distance > MaxDistance ? $"Distance cannot exceed {MaxDistance} km." : null;
    }

    private static string? ValidateText(string value, string fieldName)
    {
        string text = value.Trim();

        if (text.Length > MaxTextLength)
        {
            return $"{fieldName} cannot be longer than {MaxTextLength} characters.";
        }

        if (!text.All(IsAllowedChar))
        {
            return $"{fieldName} can contain only letters, spaces, hyphens, apostrophes and dots.";
        }

        if (text.Count(char.IsLetter) < MinLetters)
        {
            return $"{fieldName} must contain at least {MinLetters} letters.";
        }

        return null;
    }

    private static bool IsAllowedChar(char c) =>
        char.IsLetter(c) || c is ' ' or '-' or '\'' or '’' or '.';
}
