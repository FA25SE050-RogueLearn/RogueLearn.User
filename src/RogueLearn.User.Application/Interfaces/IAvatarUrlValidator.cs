namespace RogueLearn.User.Application.Interfaces;

public interface IAvatarUrlValidator
{
    /// <summary>
    /// Validates whether the provided avatar URL is allowed (scheme, domain, bucket path, etc.).
    /// </summary>
    bool IsValid(string url);
}