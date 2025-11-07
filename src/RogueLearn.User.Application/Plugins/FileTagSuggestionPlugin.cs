using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
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

        // Guidance: mirror JSON schema used by text-based tag suggestion
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

            var items = new ChatMessageContentItemCollection
            {
                new TextContent(instructions)
            };

            if (isPdf)
            {
                // Without a PDF parser, add raw content for the model to analyze
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
            else
            {
                // Fallback: send raw content if available
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
                foreach (var imagePart in slidePart.ImageParts)
                {
                    using var imageStream = imagePart.GetStream();
                    using var ms = new MemoryStream();
                    imageStream.CopyTo(ms);
                    var imageBytes = ms.ToArray();
                    if (imageBytes.Length > 0)
                    {
                        contentItems.Add(new ImageContent(imageBytes, imagePart.ContentType));
                    }
                }
                slideIndex++;
                if (slideIndex > 50) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PPTX parsing failed for tag suggestion; returning available content items only.");
        }
        return contentItems;
    }
}