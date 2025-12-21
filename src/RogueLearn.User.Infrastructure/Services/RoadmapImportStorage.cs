using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using Supabase;
using System.Text;

namespace RogueLearn.User.Infrastructure.Services;

public class RoadmapImportStorage : IRoadmapImportStorage
{
    private readonly Client _client;
    private readonly ILogger<RoadmapImportStorage> _logger;

    public RoadmapImportStorage(Client client, ILogger<RoadmapImportStorage> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SavePdfAttachmentAsync(
        string bucketName,
        string className,
        string rawTextHash,
        Stream pdfStream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storage = _client.Storage.From(bucketName);

            var safeClass = Slugify(className);
            var prefix = $"roadmap/{safeClass}/attachments/";
            var filePath = prefix + $"{rawTextHash}.pdf";

            // Read stream into bytes for upload
            using var ms = new MemoryStream();
            await pdfStream.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();

            await storage.Upload(bytes, filePath, new Supabase.Storage.FileOptions
            {
                ContentType = "application/pdf",
                Upsert = true
            });

            _logger.LogInformation("Uploaded roadmap PDF attachment to {Path} (original: {Original})", filePath, originalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload roadmap PDF attachment for class {ClassName}", className);
            throw;
        }
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "unknown-class";
        var lower = input.Trim().ToLowerInvariant();
        var sb = new StringBuilder();
        foreach (var ch in lower)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_' || ch == '/')
                sb.Append('-');
        }
        var slug = sb.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}