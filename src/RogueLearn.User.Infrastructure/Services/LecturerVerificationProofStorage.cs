using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Services;

public class LecturerVerificationProofStorage : ILecturerVerificationProofStorage
{
    private readonly Client _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LecturerVerificationProofStorage> _logger;

    public LecturerVerificationProofStorage(Client client, IConfiguration configuration, ILogger<LecturerVerificationProofStorage> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Guid authUserId, byte[] content, string? contentType, string? fileName, CancellationToken cancellationToken = default)
    {
        var bucketName = "lecturer-verification";

        string? ext = null;
        var ct = contentType?.ToLowerInvariant();
        ext = ct switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            "application/pdf" => "pdf",
            _ => null
        };

        if (ext is null && !string.IsNullOrWhiteSpace(fileName))
        {
            var fileExt = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            var allowed = new HashSet<string> { "png", "jpg", "jpeg", "webp", "gif", "pdf" };
            if (allowed.Contains(fileExt))
            {
                ext = fileExt == "jpeg" ? "jpg" : fileExt;
            }
        }

        if (ext is null)
        {
            throw new InvalidOperationException($"Unsupported proof type: {contentType}");
        }

        var fileNameFinal = $"{Guid.NewGuid()}.{ext}";
        var filePath = $"{authUserId}/{fileNameFinal}";

        var storage = _client.Storage.From(bucketName);
        var uploadContentType = contentType ?? ext switch
        {
            "png" => "image/png",
            "jpg" => "image/jpeg",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        await storage.Upload(content, filePath, new Supabase.Storage.FileOptions
        {
            ContentType = uploadContentType,
            Upsert = true
        });

        var supabaseUrl = _configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured");
        var publicUrl = $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucketName}/{filePath}";
        _logger.LogInformation("Uploaded lecturer verification proof for {AuthUserId} to {Path}", authUserId, filePath);
        return publicUrl;
    }
}