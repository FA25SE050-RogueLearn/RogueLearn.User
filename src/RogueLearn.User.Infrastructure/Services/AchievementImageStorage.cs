using BuildingBlocks.Shared.Extensions; // ToSlug
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Services;

public class AchievementImageStorage : IAchievementImageStorage
{
    private readonly Client _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AchievementImageStorage> _logger;

    public AchievementImageStorage(Client client, IConfiguration configuration, ILogger<AchievementImageStorage> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SaveIconAsync(
        string achievementName,
        Stream imageStream,
        string originalFileName,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        const string bucketName = "achievements";
        var supabaseUrl = _configuration["Supabase:Url"] ?? string.Empty;

        var storage = _client.Storage.From(bucketName);

        // Infer extension
        var ext = System.IO.Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png"; // default

        // Determine content type if not provided
        var uploadContentType = contentType ?? ext switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };

        // Build path: icons/{slug}/{timestamp}.{ext}
        var slug = achievementName.Trim().ToSlug();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var filePath = $"icons/{slug}/{timestamp}{ext}";

        // Read stream into bytes
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        await storage.Upload(bytes, filePath, new Supabase.Storage.FileOptions
        {
            ContentType = uploadContentType,
            Upsert = true
        });

        var publicUrl = $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucketName}/{filePath}";
        _logger.LogInformation("Uploaded achievement icon for '{AchievementName}' to {Path}", achievementName, filePath);
        return publicUrl;
    }
}