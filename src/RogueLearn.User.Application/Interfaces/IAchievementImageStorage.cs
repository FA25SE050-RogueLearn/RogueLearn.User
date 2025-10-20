using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Interfaces;

public interface IAchievementImageStorage
{
    /// <summary>
    /// Saves an achievement icon image to the public Supabase storage bucket and returns the public URL.
    /// Bucket: "achievements". Path convention: icons/{slug}/{timestamp}.{ext}
    /// </summary>
    /// <param name="achievementName">Achievement name used to build a slugged path</param>
    /// <param name="imageStream">Image content stream</param>
    /// <param name="originalFileName">Original filename, used to infer extension/content type</param>
    /// <param name="contentType">Optional explicit content type (e.g., image/png)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL to the uploaded icon</returns>
    Task<string> SaveIconAsync(
        string achievementName,
        Stream imageStream,
        string originalFileName,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}