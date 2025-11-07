namespace RogueLearn.User.Application.Exceptions;

/// <summary>
/// Represents an error indicating the client has sent too many requests in a given amount of time.
/// Maps to HTTP 429 Too Many Requests.
/// </summary>
public class TooManyRequestsException : Exception
{
    public TooManyRequestsException() : base("Too many requests")
    {
    }

    public TooManyRequestsException(string message) : base(message)
    {
    }

    public TooManyRequestsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}