using RogueLearn.User.Application.Interfaces;
using System.IO.Compression;
using System.Text;

namespace RogueLearn.User.Infrastructure.Services;

public class DocumentTextExtractor : IFileTextExtractor
{
    private readonly IPdfTextExtractor _pdfTextExtractor;

    public DocumentTextExtractor(IPdfTextExtractor pdfTextExtractor)
    {
        _pdfTextExtractor = pdfTextExtractor;
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string contentType, string fileName, CancellationToken cancellationToken = default)
    {
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await _pdfTextExtractor.ExtractTextAsync(fileStream, cancellationToken);
        }

        if (string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var text = await reader.ReadToEndAsync();
            return text ?? string.Empty;
        }

        // Basic DOCX extraction by reading word/document.xml and stripping tags
        if (string.Equals(contentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;
                using var archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
                var entry = archive.GetEntry("word/document.xml");
                if (entry is null) return string.Empty;
                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                var xml = await reader.ReadToEndAsync();
                var text = ExtractTextFromWordXml(xml);
                return text;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Fallback: try UTF-8
        using var fallbackReader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await fallbackReader.ReadToEndAsync() ?? string.Empty;
    }

    private static string ExtractTextFromWordXml(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return string.Empty;
        // Replace common paragraph and break tags with newlines
        var normalized = xml
            .Replace("<w:p>", "\n")
            .Replace("</w:p>", "")
            .Replace("<w:br/>", "\n")
            .Replace("<w:tab/>", "\t");
        // Strip remaining XML tags
        var sb = new StringBuilder(normalized.Length);
        bool insideTag = false;
        foreach (var ch in normalized)
        {
            if (ch == '<') { insideTag = true; continue; }
            if (ch == '>') { insideTag = false; continue; }
            if (!insideTag) sb.Append(ch);
        }
        var text = sb.ToString();
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }
}