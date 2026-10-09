using System;

namespace SmartTravelPlanner.Exceptions;

public class MapFormatException : Exception
{
    public MapFormatException()
    {
    }

    public MapFormatException(string message)
        : base(message)
    {
    }

    public MapFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
