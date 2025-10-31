namespace RogueLearn.User.Application.Exceptions;

/// <summary>
/// Represents an error where the server understands the content type of the request entity,
/// and the syntax of the request entity is correct, but it was unable to process the contained instructions.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class UnprocessableEntityException : Exception
{
    public UnprocessableEntityException() : base("Unprocessable entity")
    {
    }

    public UnprocessableEntityException(string message) : base(message)
    {
    }

    public UnprocessableEntityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}