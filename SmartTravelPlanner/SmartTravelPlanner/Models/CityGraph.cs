using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using SmartTravelPlanner.Exceptions;

namespace SmartTravelPlanner.Models;

public class CityGraph
{
    private const string SpacedSeparator = " - ";

    private readonly Dictionary<string, CityNode> _cities = new(StringComparer.OrdinalIgnoreCase);

    public static CityGraph LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new MapFormatException("Map file not found.");
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new MapFormatException("Map file cannot be read.", ex);
        }

        var graph = new CityGraph();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            (string city1, string city2, int distance) = ParseConnection(line, i + 1);
            graph.AddEdge(city1, city2, distance);
        }

        if (graph._cities.Count == 0)
        {
            throw new MapFormatException("Map file has an invalid format: it contains no connections.");
        }

        return graph;
    }

    public void AddEdge(string city1, string city2, int distance)
    {
        string first = city1?.Trim() ?? string.Empty;
        string second = city2?.Trim() ?? string.Empty;

        if (first.Length == 0)
        {
            throw new ArgumentException("City name cannot be empty.", nameof(city1));
        }

        if (second.Length == 0)
        {
            throw new ArgumentException("City name cannot be empty.", nameof(city2));
        }

        if (string.Equals(first, second, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A city cannot be connected to itself.", nameof(city2));
        }

        if (distance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Distance must be greater than zero.");
        }

        CityNode firstNode = GetOrAddCity(first);
        CityNode secondNode = GetOrAddCity(second);

        firstNode.Neighbors[secondNode.Name] = distance;
        secondNode.Neighbors[firstNode.Name] = distance;
    }

    public bool RemoveEdge(string city1, string city2)
    {
        if (!TryGetCity(city1, out CityNode? first) || !TryGetCity(city2, out CityNode? second))
        {
            return false;
        }

        bool removed = first.Neighbors.Remove(second.Name);
        second.Neighbors.Remove(first.Name);

        if (first.Neighbors.Count == 0)
        {
            _cities.Remove(first.Name);
        }

        if (second.Neighbors.Count == 0)
        {
            _cities.Remove(second.Name);
        }

        return removed;
    }

    public IReadOnlyList<Edge> GetAllEdges()
    {
        var edges = new List<Edge>();

        foreach (CityNode city in _cities.Values)
        {
            foreach (KeyValuePair<string, int> neighbor in city.Neighbors)
            {
                if (string.Compare(city.Name, neighbor.Key, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    edges.Add(new Edge(city.Name, neighbor.Key, neighbor.Value));
                }
            }
        }

        edges.Sort((a, b) =>
        {
            int byFirst = string.Compare(a.City1, b.City1, StringComparison.OrdinalIgnoreCase);
            return byFirst != 0 ? byFirst : string.Compare(a.City2, b.City2, StringComparison.OrdinalIgnoreCase);
        });

        return edges;
    }

    public bool ContainsCity(string? city) => TryGetCity(city, out _);

    public CityGraph Clone()
    {
        var copy = new CityGraph();

        foreach (CityNode city in _cities.Values)
        {
            copy.GetOrAddCity(city.Name);
        }

        foreach (Edge edge in GetAllEdges())
        {
            copy.AddEdge(edge.City1, edge.City2, edge.Distance);
        }

        return copy;
    }

    public List<string> FindShortestPath(string from, string to)
    {
        var path = new List<string>();

        if (!TryGetCity(from, out CityNode? start) || !TryGetCity(to, out CityNode? target))
        {
            return path;
        }

        var distances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { [start.Name] = 0 };
        var previous = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new PriorityQueue<CityNode, int>();
        queue.Enqueue(start, 0);

        while (queue.TryDequeue(out CityNode? current, out int currentDistance))
        {
            if (!visited.Add(current.Name))
            {
                continue;
            }

            if (string.Equals(current.Name, target.Name, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            foreach (KeyValuePair<string, int> neighbor in current.Neighbors)
            {
                if (visited.Contains(neighbor.Key))
                {
                    continue;
                }

                long sum = (long)currentDistance + neighbor.Value;
                if (sum > int.MaxValue)
                {
                    continue;
                }

                int candidate = (int)sum;
                if (!distances.TryGetValue(neighbor.Key, out int known) || candidate < known)
                {
                    distances[neighbor.Key] = candidate;
                    previous[neighbor.Key] = current.Name;
                    queue.Enqueue(_cities[neighbor.Key], candidate);
                }
            }
        }

        if (!distances.ContainsKey(target.Name))
        {
            return path;
        }

        string? step = target.Name;
        while (step is not null)
        {
            path.Add(step);
            step = previous.GetValueOrDefault(step);
        }

        path.Reverse();
        return path;
    }

    public int GetPathDistance(IReadOnlyList<string>? path)
    {
        if (path is null || path.Count == 0)
        {
            return 0;
        }

        if (path.Count == 1)
        {
            return ContainsCity(path[0]) ? 0 : -1;
        }

        long total = 0;

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (!TryGetCity(path[i], out CityNode? current) ||
                !current.Neighbors.TryGetValue(path[i + 1], out int distance))
            {
                return -1;
            }

            total += distance;
            if (total > int.MaxValue)
            {
                return -1;
            }
        }

        return (int)total;
    }

    private static (string City1, string City2, int Distance) ParseConnection(string line, int lineNumber)
    {
        int commaIndex = line.LastIndexOf(',');
        if (commaIndex < 0)
        {
            throw InvalidLine(lineNumber, "a comma before the distance is missing");
        }

        string distancePart = line[(commaIndex + 1)..].Trim();
        if (!int.TryParse(distancePart, NumberStyles.None, CultureInfo.InvariantCulture, out int distance) || distance <= 0)
        {
            throw InvalidLine(lineNumber, "the distance must be a whole number greater than zero");
        }

        if (!TrySplitCities(line[..commaIndex], out string city1, out string city2))
        {
            throw InvalidLine(
                lineNumber,
                "two city names separated by '-' are expected (use ' - ' with spaces if a name contains a hyphen)");
        }

        if (string.Equals(city1, city2, StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidLine(lineNumber, "a city cannot be connected to itself");
        }

        return (city1, city2, distance);
    }

    private static bool TrySplitCities(string citiesPart, out string city1, out string city2)
    {
        city1 = string.Empty;
        city2 = string.Empty;

        int separatorIndex = citiesPart.IndexOf(SpacedSeparator, StringComparison.Ordinal);
        string left;
        string right;

        if (separatorIndex >= 0)
        {
            left = citiesPart[..separatorIndex];
            right = citiesPart[(separatorIndex + SpacedSeparator.Length)..];

            if (right.Contains(SpacedSeparator, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else
        {
            int hyphenIndex = citiesPart.IndexOf('-');
            if (hyphenIndex < 0 || hyphenIndex != citiesPart.LastIndexOf('-'))
            {
                return false;
            }

            left = citiesPart[..hyphenIndex];
            right = citiesPart[(hyphenIndex + 1)..];
        }

        left = left.Trim();
        right = right.Trim();

        if (left.Length == 0 || right.Length == 0)
        {
            return false;
        }

        city1 = left;
        city2 = right;
        return true;
    }

    private static MapFormatException InvalidLine(int lineNumber, string reason) =>
        new($"Map file has an invalid format (line {lineNumber}: {reason}). Expected format: City1-City2,distance");

    private CityNode GetOrAddCity(string name)
    {
        if (!_cities.TryGetValue(name, out CityNode? city))
        {
            city = new CityNode(name);
            _cities[name] = city;
        }

        return city;
    }

    private bool TryGetCity(string? name, [NotNullWhen(true)] out CityNode? city)
    {
        city = null;
        return !string.IsNullOrWhiteSpace(name) && _cities.TryGetValue(name.Trim(), out city);
    }

    private sealed class CityNode(string name)
    {
        public string Name { get; } = name;

        public Dictionary<string, int> Neighbors { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
