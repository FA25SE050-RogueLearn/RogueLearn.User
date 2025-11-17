// RogueLearn.User/src/RogueLearn.User.Application/Interfaces/IUrlValidationService.cs
namespace RogueLearn.User.Application.Interfaces;

/// <summary>
/// Defines a service for validating the accessibility of a URL.
/// </summary>
public interface IUrlValidationService
{
    /// <summary>
    /// Checks if a given URL is live and returns a success status code.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the URL is accessible; otherwise, false.</returns>
    Task<bool> IsUrlAccessibleAsync(string url, CancellationToken cancellationToken = default);
}