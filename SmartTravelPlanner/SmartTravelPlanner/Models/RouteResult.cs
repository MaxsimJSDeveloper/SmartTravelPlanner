using System.Collections.Generic;

namespace SmartTravelPlanner.Models;

public class RouteResult
{
    public bool IsSuccess { get; init; }

    public IReadOnlyList<string> Path { get; init; } = [];

    public int TotalDistance { get; init; }

    public string? ErrorMessage { get; init; }

    public static RouteResult Success(IReadOnlyList<string> path, int distance) =>
        new() { IsSuccess = true, Path = path, TotalDistance = distance };

    public static RouteResult Fail(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
