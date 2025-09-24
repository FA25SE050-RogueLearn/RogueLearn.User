namespace RogueLearn.User.Domain.Exceptions;

public class InvalidPriceException : Exception
{
    public InvalidPriceException() : base("Price cannot be negative or zero")
    {
    }

    public InvalidPriceException(string message) : base(message)
    {
    }

    public InvalidPriceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}