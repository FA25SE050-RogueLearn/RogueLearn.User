using Microsoft.Extensions.Configuration;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

public class AvatarUrlValidator : IAvatarUrlValidator
{
    private readonly IConfiguration _configuration;

    public AvatarUrlValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsValid(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        var allowedHosts = _configuration.GetSection("Avatar:AllowedHosts").Get<string[]>() ?? Array.Empty<string>();
        if (allowedHosts.Length > 0)
        {
            var hostMatch = allowedHosts.Any(h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase));
            if (!hostMatch) return false;
        }

        var supabaseBucket = _configuration.GetValue<string>("Avatar:SupabasePublicBucketName");
        if (!string.IsNullOrEmpty(supabaseBucket))
        {
            // Typical Supabase public URL structure: /storage/v1/object/public/{bucket}/...
            var path = uri.AbsolutePath.Replace("\\", "/");
            if (!path.Contains($"/storage/v1/object/public/{supabaseBucket}/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}