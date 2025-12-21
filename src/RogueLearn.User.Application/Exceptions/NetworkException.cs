namespace RogueLearn.User.Application.Exceptions;

/// <summary>
/// Thrown when a network or connectivity error occurs
/// </summary>
public class NetworkException : Exception
{
    public NetworkException(string message) : base(message) { }

    public NetworkException(string message, Exception innerException)
        : base(message, innerException) { }
}