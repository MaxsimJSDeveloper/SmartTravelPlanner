using System;

namespace SmartTravelPlanner.Exceptions;

public class TravelerFileException : Exception
{
    public TravelerFileException()
    {
    }

    public TravelerFileException(string message)
        : base(message)
    {
    }

    public TravelerFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
