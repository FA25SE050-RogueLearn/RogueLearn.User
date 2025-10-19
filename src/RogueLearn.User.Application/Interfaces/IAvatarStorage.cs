namespace RogueLearn.User.Application.Interfaces;

public interface IAvatarStorage
{
    /// <summary>
    /// Saves a user's avatar image to the public Supabase storage bucket and returns the public URL.
    /// Expected path: {authUserId}/avatar.{ext}
    /// </summary>
    /// <param name="authUserId">Authenticated user's UUID</param>
    /// <param name="imageBytes">Image bytes</param>
    /// <param name="contentType">Optional content type of the image (image/png, image/jpeg, image/webp, image/gif)</param>
    /// <param name="originalFileName">Optional original file name used for extension fallback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL to the stored avatar</returns>
    Task<string> SaveAvatarAsync(
        Guid authUserId,
        byte[] imageBytes,
        string? contentType,
        string? originalFileName,
        CancellationToken cancellationToken = default);
}