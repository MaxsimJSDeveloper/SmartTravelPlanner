namespace SmartTravelPlanner.Models;
 
public sealed record Edge(string City1, string City2, int Distance)
{
    public string DisplayText => $"{City1} ↔ {City2}: {Distance} km";
 
    public override string ToString() => DisplayText;
}
