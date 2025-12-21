using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Services;

public class AvatarStorage : IAvatarStorage
{
    private readonly Client _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AvatarStorage> _logger;

    public AvatarStorage(Client client, IConfiguration configuration, ILogger<AvatarStorage> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SaveAvatarAsync(Guid authUserId, byte[] imageBytes, string? contentType, string? originalFileName, CancellationToken cancellationToken = default)
    {
        var bucketName = "user-avatars";

        string? ext = null;
        var ct = contentType?.ToLowerInvariant();
        ext = ct switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => null
        };

        if (ext is null && !string.IsNullOrWhiteSpace(originalFileName))
        {
            var fileExt = Path.GetExtension(originalFileName).TrimStart('.').ToLowerInvariant();
            var allowedExts = new HashSet<string> { "png", "jpg", "jpeg", "webp", "gif" };
            if (allowedExts.Contains(fileExt))
            {
                ext = fileExt == "jpeg" ? "jpg" : fileExt;
            }
        }

        if (ext is null)
        {
            throw new InvalidOperationException($"Unsupported avatar image type: {contentType}");
        }

        var filePath = $"{authUserId}/avatar.{ext}";

        try
        {
            var storage = _client.Storage.From(bucketName);

            var uploadContentType = contentType ?? ext switch
            {
                "png" => "image/png",
                "jpg" => "image/jpeg",
                "webp" => "image/webp",
                "gif" => "image/gif",
                _ => "application/octet-stream"
            };

            await storage.Upload(imageBytes, filePath, new Supabase.Storage.FileOptions
            {
                ContentType = uploadContentType,
                Upsert = true
            });

            var supabaseUrl = _configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured");
            var publicUrl = $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucketName}/{filePath}";

            _logger.LogInformation("Uploaded user avatar for {AuthUserId} to {Path}", authUserId, filePath);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload user avatar for {AuthUserId}", authUserId);
            throw;
        }
    }
}