using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using System.Text;
using System.Text.Json;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Generates tag suggestions directly from an uploaded file by sending it to the configured AI via Semantic Kernel.
/// Returns ONLY a JSON string matching the expected schema.
/// </summary>
public class FileTagSuggestionPlugin : IFileTagSuggestionPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<FileTagSuggestionPlugin> _logger;

    public FileTagSuggestionPlugin(Kernel kernel, ILogger<FileTagSuggestionPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> GenerateTagSuggestionsJsonAsync(AiFileAttachment attachment, int maxTags = 10, CancellationToken cancellationToken = default)
    {
        if (attachment == null)
        {
            return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
        }

        var clampedMax = Math.Max(1, Math.Min(20, maxTags));
        var systemPrompt = "You are a helpful assistant that extracts topic tags from an attached file.";

        var instructions = $@"Analyze the attached file and propose a concise list of topic tags.
Return ONLY JSON following this exact schema:

{{
  ""tags"": [
    {{ ""label"": ""string"", ""confidence"": number (0.0-1.0), ""reason"": ""short explanation (<=120 chars)"" }}
  ]
}}

Rules:
- Use 1-{clampedMax} tags max, ordered by confidence.
- Prefer simple, commonly-used labels users would recognize (e.g., 'C#', 'ASP.NET Core', 'JWT').
- Avoid duplicates and near-duplicates: normalize case, treat hyphen/space variations as same (e.g., dotnet, .NET).
- Do NOT include markdown fences or commentary.
";

        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory(systemPrompt);

            var contentType = (attachment.ContentType ?? string.Empty).ToLowerInvariant();
            var fileName = attachment.FileName ?? string.Empty;
            var isPdf = contentType.Contains("application/pdf") || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            var isPptx = contentType.Contains("application/vnd.openxmlformats-officedocument.presentationml.presentation") || fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);
            var isDocx = contentType.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document") || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

            var items = new ChatMessageContentItemCollection
            {
                new TextContent(instructions)
            };

            if (isPdf)
            {
                if (attachment.Bytes is { Length: > 0 })
                {
                    items.Add(new ImageContent(new ReadOnlyMemory<byte>(attachment.Bytes), attachment.ContentType));
                }
                else if (attachment.Stream != null)
                {
                    using var ms = new MemoryStream();
                    await attachment.Stream.CopyToAsync(ms, cancellationToken);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        items.Add(new ImageContent(new ReadOnlyMemory<byte>(bytes), attachment.ContentType));
                    }
                }
            }
            else if (isPptx)
            {
                Stream pptxStream = attachment.Stream ?? (attachment.Bytes != null ? new MemoryStream(attachment.Bytes) : Stream.Null);
                if (pptxStream == Stream.Null) return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
                if (pptxStream.CanSeek) pptxStream.Position = 0;
                var parsedItems = ProcessPowerPoint(pptxStream);
                foreach (var it in parsedItems) items.Add(it);
            }
            else if (isDocx)
            {
                Stream docxStream = attachment.Stream ?? (attachment.Bytes != null ? new MemoryStream(attachment.Bytes) : Stream.Null);
                if (docxStream == Stream.Null) return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
                if (docxStream.CanSeek) docxStream.Position = 0;
                var parsedItems = ProcessWordDocument(docxStream);
                foreach (var it in parsedItems) items.Add(it);
            }
            else
            {
                if (attachment.Bytes != null && attachment.Bytes.Length > 0)
                {
                    items.Add(new ImageContent(new ReadOnlyMemory<byte>(attachment.Bytes), attachment.ContentType));
                }
                else if (attachment.Stream != null)
                {
                    using var ms = new MemoryStream();
                    await attachment.Stream.CopyToAsync(ms, cancellationToken);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        items.Add(new ImageContent(new ReadOnlyMemory<byte>(bytes), attachment.ContentType));
                    }
                }
            }

            chatHistory.AddUserMessage(items);

            var reply = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var rawResponse = reply?.Content?.ToString() ?? string.Empty;
            _logger.LogInformation("File tag suggestion raw response: {RawResponse}", rawResponse);
            var cleaned = CleanToJson(rawResponse);
            if (!string.IsNullOrWhiteSpace(cleaned))
                return cleaned;
            var fallbackText = ExtractPlainText(attachment);
            return BuildFallbackTagsJson(fallbackText, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tag suggestions from file via AI. FileName={FileName}, ContentType={ContentType}", attachment.FileName, attachment.ContentType);
            var fallbackText = ExtractPlainText(attachment);
            return BuildFallbackTagsJson(fallbackText, Array.Empty<string>());
        }
    }

    public async Task<string> GenerateTagSuggestionsJsonAsync(AiFileAttachment attachment, IEnumerable<string> knownTags, int maxTags = 10, CancellationToken cancellationToken = default)
    {
        if (attachment == null)
        {
            return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
        }

        var clampedMax = Math.Max(1, Math.Min(20, maxTags));
        var systemPrompt = "You are a helpful assistant that extracts topic tags from an attached file.";

        var known = (knownTags ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToArray();

        var knownList = known.Length > 0 ? string.Join(", ", known) : string.Empty;
        var guidance = known.Length > 0
            ? $"\nPrefer selecting tags from this allowed list when relevant: [{knownList}]\nIf none match, propose concise new tags."
            : string.Empty;

        var instructions = $@"Analyze the attached file and propose a concise list of topic tags.
Return ONLY JSON following this exact schema:

{{
  ""tags"": [
    {{ ""label"": ""string"", ""confidence"": number (0.0-1.0), ""reason"": ""short explanation (<=120 chars)"" }}
  ]
}}

Rules:
- Use 1-{clampedMax} tags max, ordered by confidence.
- Prefer simple, commonly-used labels users would recognize.
- Avoid duplicates and near-duplicates: normalize case; treat hyphen/space/punctuation variants as same.
- Prefer singular forms (e.g., challenge over challenges).
- Normalize common tech synonyms (e.g., .NET->dotnet, C#->csharp).
- Do NOT include markdown fences or commentary.{guidance}
";

        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory(systemPrompt);

            var contentType = (attachment.ContentType ?? string.Empty).ToLowerInvariant();
            var fileName = attachment.FileName ?? string.Empty;
            var isPdf = contentType.Contains("application/pdf") || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            var isPptx = contentType.Contains("application/vnd.openxmlformats-officedocument.presentationml.presentation") || fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);
            var isDocx = contentType.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document") || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

            var items = new ChatMessageContentItemCollection { new TextContent(instructions) };

            if (isPdf)
            {
                if (attachment.Bytes is { Length: > 0 })
                {
                    items.Add(new ImageContent(new ReadOnlyMemory<byte>(attachment.Bytes), attachment.ContentType));
                }
                else if (attachment.Stream != null)
                {
                    using var ms = new MemoryStream();
                    await attachment.Stream.CopyToAsync(ms, cancellationToken);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        items.Add(new ImageContent(new ReadOnlyMemory<byte>(bytes), attachment.ContentType));
                    }
                }
            }
            else if (isPptx)
            {
                var s = attachment.Stream ?? (attachment.Bytes != null ? new MemoryStream(attachment.Bytes) : Stream.Null);
                if (s == Stream.Null) return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
                if (s.CanSeek) s.Position = 0;
                var parsedItems = ProcessPowerPoint(s);
                foreach (var it in parsedItems) items.Add(it);
            }
            else if (isDocx)
            {
                var s = attachment.Stream ?? (attachment.Bytes != null ? new MemoryStream(attachment.Bytes) : Stream.Null);
                if (s == Stream.Null) return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
                if (s.CanSeek) s.Position = 0;
                var parsedItems = ProcessWordDocument(s);
                foreach (var it in parsedItems) items.Add(it);
            }
            else
            {
                if (attachment.Bytes != null && attachment.Bytes.Length > 0)
                {
                    items.Add(new ImageContent(new ReadOnlyMemory<byte>(attachment.Bytes), attachment.ContentType));
                }
                else if (attachment.Stream != null)
                {
                    using var ms = new MemoryStream();
                    await attachment.Stream.CopyToAsync(ms, cancellationToken);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        items.Add(new ImageContent(new ReadOnlyMemory<byte>(bytes), attachment.ContentType));
                    }
                }
            }

            chatHistory.AddUserMessage(items);
            var reply = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var rawResponse = reply?.Content?.ToString() ?? string.Empty;
            _logger.LogInformation("File tag suggestion raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tag suggestions from file via AI. FileName={FileName}, ContentType={ContentType}", attachment.FileName, attachment.ContentType);
            return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
        }
    }

    private static string CleanToJson(string rawResponse)
    {
        var cleanedResponse = rawResponse.Trim();
        if (cleanedResponse.StartsWith("```"))
        {
            var firstNewline = cleanedResponse.IndexOf('\n');
            if (firstNewline > -1)
            {
                cleanedResponse = cleanedResponse[(firstNewline + 1)..];
            }
        }
        if (cleanedResponse.EndsWith("```") && cleanedResponse.Length >= 3)
        {
            var lastFenceIndex = cleanedResponse.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex > -1)
            {
                cleanedResponse = cleanedResponse[..lastFenceIndex];
            }
        }
        var startIdx = cleanedResponse.IndexOf('{');
        var endIdx = cleanedResponse.LastIndexOf('}');
        if (startIdx >= 0 && endIdx > startIdx)
        {
            cleanedResponse = cleanedResponse.Substring(startIdx, endIdx - startIdx + 1);
        }
        return cleanedResponse.Trim();
    }

    // Note: PDF parsing is not implemented due to missing library support in this environment.

    private ChatMessageContentItemCollection ProcessPowerPoint(Stream pptxStream)
    {
        var contentItems = new ChatMessageContentItemCollection();
        try
        {
            using var presentationDocument = PresentationDocument.Open(pptxStream, false);
            var presentationPart = presentationDocument.PresentationPart;
            if (presentationPart == null) return contentItems;
            int slideIndex = 1;
            foreach (var slidePart in presentationPart.SlideParts)
            {
                contentItems.Add(new TextContent($"--- Content from Slide {slideIndex} ---"));
                var slideText = new System.Text.StringBuilder();
                var textNodes = slidePart.Slide.Descendants<Drawing.Text>();
                foreach (var textNode in textNodes)
                {
                    slideText.Append(textNode.Text).Append(' ');
                }
                if (slideText.Length > 0)
                {
                    contentItems.Add(new TextContent(slideText.ToString()));
                }
                slideIndex++;
                if (slideIndex > 20) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PPTX parsing failed for tag suggestion; returning available content items only.");
        }
        return contentItems;
    }

    private ChatMessageContentItemCollection ProcessWordDocument(Stream docxStream)
    {
        var contentItems = new ChatMessageContentItemCollection();
        try
        {
            using var wordDoc = WordprocessingDocument.Open(docxStream, false);
            var main = wordDoc.MainDocumentPart;
            var body = main?.Document?.Body;
            if (body != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    foreach (var t in paragraph.Descendants<Text>())
                    {
                        sb.Append(t.Text).Append(' ');
                    }
                    sb.AppendLine();
                }
                var text = sb.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    contentItems.Add(new TextContent(text));
                }
            }
            if (main != null)
            {
                foreach (var imagePart in main.ImageParts)
                {
                    using var imageStream = imagePart.GetStream();
                    using var ms = new MemoryStream();
                    imageStream.CopyTo(ms);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        contentItems.Add(new ImageContent(bytes, imagePart.ContentType));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DOCX parsing failed for tag suggestion; returning available content items only.");
        }
        return contentItems;
    }

    private static string ExtractPlainText(AiFileAttachment attachment)
    {
        var contentType = (attachment.ContentType ?? string.Empty).ToLowerInvariant();
        var fileName = attachment.FileName ?? string.Empty;
        var isPptx = contentType.Contains("application/vnd.openxmlformats-officedocument.presentationml.presentation") || fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);
        var isDocx = contentType.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document") || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

        try
        {
            if (isPptx)
            {
                var s = attachment.Stream ?? (attachment.Bytes != null ? new MemoryStream(attachment.Bytes) : Stream.Null);
                if (s == Stream.Null) return string.Empty;
                if (s.CanSeek) s.Position = 0;
                using var presentationDocument = PresentationDocument.Open(s, false);
                var presentationPart = presentationDocument.PresentationPart;
                if (presentationPart == null) return string.Empty;
                var sb = new StringBuilder();
                int slideIndex = 1;
                foreach (var slidePart in presentationPart.SlideParts)
                {
                    var textNodes = slidePart.Slide.Descendants<Drawing.Text>();
                    foreach (var t in textNodes) sb.Append(t.Text).Append(' ');
                    sb.AppendLine();
                    slideIndex++;
                    if (slideIndex > 20) break;
                }
                return sb.ToString().Trim();
            }
            if (isDocx)
            {
                var s = attachment.Stream ?? (attachment.Bytes != null ? new MemoryStream(attachment.Bytes) : Stream.Null);
                if (s == Stream.Null) return string.Empty;
                if (s.CanSeek) s.Position = 0;
                using var wordDoc = WordprocessingDocument.Open(s, false);
                var main = wordDoc.MainDocumentPart;
                var body = main?.Document?.Body;
                if (body == null) return string.Empty;
                var sb = new StringBuilder();
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    foreach (var t in paragraph.Descendants<Text>()) sb.Append(t.Text).Append(' ');
                    sb.AppendLine();
                }
                return sb.ToString().Trim();
            }
            if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("text/"))
            {
                using var reader = new StreamReader(attachment.Stream ?? new MemoryStream(attachment.Bytes ?? Array.Empty<byte>()));
                return reader.ReadToEnd();
            }
        }
        catch { }
        return string.Empty;
    }

    private static string BuildFallbackTagsJson(string text, IEnumerable<string> knownTags, int maxTags = 10)
    {
        var max = Math.Max(1, Math.Min(20, maxTags));
        if (string.IsNullOrWhiteSpace(text)) return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });

        var stop = new HashSet<string>(new[]
        {
            "the","and","or","for","with","without","a","an","of","in","on","to","from","by","at","as","is","are","was","were","be","been","being"
        });

        var words = text
            .ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => w.Length >= 3 && !stop.Contains(w))
            .Select(NormalizeLabel)
            .Where(w => w.Length >= 3)
            .ToList();

        var freq = words
            .GroupBy(w => w)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var known = (knownTags ?? Array.Empty<string>())
            .Select(k => k.Trim())
            .Where(k => k.Length > 0)
            .ToList();

        var knownMap = known
            .ToDictionary(k => NormalizeLabel(k), k => k, StringComparer.OrdinalIgnoreCase);

        var selected = new List<(string Label, double Confidence, string Reason)>();

        foreach (var item in freq)
        {
            string label = item.Label;
            if (knownMap.TryGetValue(label, out var canonical))
            {
                selected.Add((canonical, Math.Min(1.0, 0.6 + item.Count * 0.02), "Matches existing tag"));
            }
            else
            {
                selected.Add((label, Math.Min(1.0, 0.5 + item.Count * 0.02), "Frequent keyword"));
            }
            if (selected.Count >= max) break;
        }

        var payload = new
        {
            tags = selected.Select(s => new { label = s.Label, confidence = s.Confidence, reason = s.Reason }).ToArray()
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string NormalizeLabel(string label)
    {
        var s = (label ?? string.Empty).Trim().ToLowerInvariant();
        if (s.Length == 0) return s;
        if (s.EndsWith("ies") && s.Length > 3) s = s[..^3] + "y";
        else if (s.EndsWith("es") && s.Length > 4) s = s[..^2];
        else if (s.EndsWith("s") && s.Length > 3) s = s[..^1];
        s = s.Replace(".net", "dotnet");
        s = s.Replace("c#", "csharp");
        return s;
    }
}