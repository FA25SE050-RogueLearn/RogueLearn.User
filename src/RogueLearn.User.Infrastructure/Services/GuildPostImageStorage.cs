using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Services;

public class GuildPostImageStorage : IGuildPostImageStorage
{
    private readonly Client _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GuildPostImageStorage> _logger;

    public GuildPostImageStorage(Client client, IConfiguration configuration, ILogger<GuildPostImageStorage> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> SaveImagesAsync(Guid guildId, Guid postId, IEnumerable<(byte[] Content, string? ContentType, string? FileName)> files, CancellationToken cancellationToken = default)
    {
        var bucketName = "guild-posts";
        var results = new List<string>();

        foreach (var f in files)
        {
            string? ext = null;
            var ct = f.ContentType?.ToLowerInvariant();
            ext = ct switch
            {
                "image/png" => "png",
                "image/jpeg" => "jpg",
                "image/jpg" => "jpg",
                "image/webp" => "webp",
                "image/gif" => "gif",
                _ => null
            };

            if (ext is null && !string.IsNullOrWhiteSpace(f.FileName))
            {
                var fileExt = Path.GetExtension(f.FileName).TrimStart('.').ToLowerInvariant();
                var allowed = new HashSet<string> { "png", "jpg", "jpeg", "webp", "gif" };
                if (allowed.Contains(fileExt))
                {
                    ext = fileExt == "jpeg" ? "jpg" : fileExt;
                }
            }

            if (ext is null)
            {
                throw new InvalidOperationException($"Unsupported image type: {f.ContentType}");
            }

            var fileName = $"{Guid.NewGuid()}.{ext}";
            var filePath = $"{guildId}/{postId}/{fileName}";

            try
            {
                var storage = _client.Storage.From(bucketName);
                var uploadContentType = f.ContentType ?? ext switch
                {
                    "png" => "image/png",
                    "jpg" => "image/jpeg",
                    "webp" => "image/webp",
                    "gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                await storage.Upload(f.Content, filePath, new Supabase.Storage.FileOptions
                {
                    ContentType = uploadContentType,
                    Upsert = true
                });

                var supabaseUrl = _configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured");
                var publicUrl = $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucketName}/{filePath}";
                results.Add(publicUrl);
                _logger.LogInformation("Uploaded guild post image for {GuildId}/{PostId} to {Path}", guildId, postId, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload guild post image for {GuildId}/{PostId}", guildId, postId);
                throw;
            }
        }

        return results;
    }

    public async Task DeleteByUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var bucketName = "guild-posts";
        var storage = _client.Storage.From(bucketName);
        var supabaseUrl = _configuration["Supabase:Url"] ?? string.Empty;
        var prefix = $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucketName}/";

        foreach (var url in urls)
        {
            try
            {
                var path = url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    ? url.Substring(prefix.Length)
                    : null;
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }
                await storage.Remove(new List<string> { path });
                _logger.LogInformation("Deleted guild post image {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete guild post image {Url}", url);
            }
        }
    }
}