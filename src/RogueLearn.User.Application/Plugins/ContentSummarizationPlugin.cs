using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using DocumentFormat.OpenXml.Packaging;
using Drawing = DocumentFormat.OpenXml.Drawing;
using System.Text;
using System.Text.Json;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Summarization plugin using the configured Semantic Kernel model.
/// For files, it extracts text server-side and then summarizes it.
/// </summary>
public class ContentSummarizationPlugin : ISummarizationPlugin, IFileSummarizationPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<ContentSummarizationPlugin> _logger;

    // System instruction to force the model to return BlockNote-compatible JSON only
    private const string BlockNoteJsonInstruction =
      @"You are a content summarizer that outputs ONLY a valid JSON array of BlockNote blocks.
Follow these rules strictly:
1) Return ONLY a JSON array (no prose, no markdown fences, no explanations).
2) The root MUST be an array of BlockNote blocks. Do NOT wrap in objects (e.g., no {""blocks"": [...] }).
3) Use BlockNote built-in blocks: heading, paragraph, bulletListItem, numberedListItem, checkListItem, quote, codeBlock, image, table.
4) Each block MUST be an object with: id (UUID v4), type, props { backgroundColor: 'default', textColor: 'default', textAlignment: 'left' }, content: [ inline items such as { type: 'text', text: '...', styles: {} } ], children: [].
5) Keep to <= 20 blocks total. Avoid empty blocks. The output MUST NOT be an empty array.
6) ALWAYS include at least one paragraph block with non-empty text content.
7) Prefer a concise structured summary: a top heading (e.g., 'Summary'), short sections as 'heading', and key points as 'bulletListItem'.
8) Do NOT include code fences like ```json or any additional commentary.
9) For images found in slides, include image blocks only if meaningful; otherwise omit.
10) Ensure the JSON is syntactically valid and can be parsed without modification.";

    public ContentSummarizationPlugin(Kernel kernel, ILogger<ContentSummarizationPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<object?> SummarizeTextAsync(string rawText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return null;
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(BlockNoteJsonInstruction);

            var userItems = new ChatMessageContentItemCollection
      {
        new TextContent("Summarize the following content into BlockNote JSON blocks with clear headings and bullet points. If the content is minimal, STILL return a non-empty array with at least one paragraph block containing a concise summary:"),
        new TextContent(rawText)
      };
            chatHistory.AddUserMessage(userItems);

            var result = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var aiOutput = result?.Content?.ToString() ?? string.Empty;
            var sanitized = SanitizeJsonOutput(aiOutput);
            _logger.LogInformation("AI raw output for text summarization: {RawOutput}", aiOutput);
            _logger.LogInformation("AI sanitized output for text summarization: {SanitizedOutput}", sanitized);
            var parsed = TryParseBlockNoteJsonToObject(sanitized);
            if (parsed is { } obj && obj is List<object> list && list.Count == 0)
            {
                // avoid empty array — construct minimal paragraph block
                parsed = BlockNoteDocumentFactory.FromPlainText(rawText.Trim());
            }
            if (parsed is null)
            {
                // Fallback to plain text conversion
                parsed = BlockNoteDocumentFactory.FromPlainText(rawText.Trim());
            }
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to summarize text");
            return null;
        }
    }

    public async Task<object?> SummarizeAsync(AiFileAttachment attachment, CancellationToken cancellationToken = default)
    {
        if (attachment == null) return null;

        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            // Decide parser based on MIME or file extension
            var contentType = (attachment.ContentType ?? string.Empty).ToLowerInvariant();
            var fileName = attachment.FileName ?? string.Empty;
            var isPdf = contentType.Contains("application/pdf") || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            var isPptx = contentType.Contains("application/vnd.openxmlformats-officedocument.presentationml.presentation") || fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);

            ChatMessageContentItemCollection contentItems;
            if (isPdf)
            {
                // Fallback: without a PDF parser library available, send the raw bytes/content to the model
                contentItems = new ChatMessageContentItemCollection();
                if (attachment.Bytes is { Length: > 0 })
                {
                    contentItems.Add(new ImageContent(new ReadOnlyMemory<byte>(attachment.Bytes), attachment.ContentType));
                }
                else if (attachment.Stream != null)
                {
                    using var ms = new MemoryStream();
                    attachment.Stream.CopyTo(ms);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        contentItems.Add(new ImageContent(new ReadOnlyMemory<byte>(bytes), attachment.ContentType));
                    }
                }
            }
            else if (isPptx)
            {
                Stream pptxStream = attachment.Stream ?? (attachment.Bytes != null ? new MemoryStream(attachment.Bytes) : Stream.Null);
                if (pptxStream == Stream.Null) return null;
                if (pptxStream.CanSeek) pptxStream.Position = 0;
                contentItems = ProcessPowerPoint(pptxStream);
            }
            else
            {
                // Fallback: for other types, if bytes are present send as attachment content
                // Or if it's a text content type, attempt a simple text read
                contentItems = new ChatMessageContentItemCollection();
                if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("text/"))
                {
                    // Read text from stream/bytes
                    using var reader = new StreamReader(attachment.Stream ?? new MemoryStream(attachment.Bytes ?? Array.Empty<byte>()));
                    var text = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        contentItems.Add(new TextContent(text));
                    }
                }
                else if (attachment.Bytes != null && attachment.Bytes.Length > 0)
                {
                    contentItems.Add(new ImageContent(new ReadOnlyMemory<byte>(attachment.Bytes), contentType));
                }
                else if (attachment.Stream != null)
                {
                    using var ms = new MemoryStream();
                    attachment.Stream.CopyTo(ms);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        contentItems.Add(new ImageContent(new ReadOnlyMemory<byte>(bytes), contentType));
                    }
                }
            }

            if (contentItems.Count == 0)
            {
                return null;
            }

            var chatHistory = new ChatHistory();
            // System instruction to force JSON BlockNote output
            chatHistory.AddSystemMessage(BlockNoteJsonInstruction);

            // Final instruction + content payload for the AI summarization
            var finalPrompt = new ChatMessageContentItemCollection
      {
        new TextContent("Based on the following content from a document (which may include text and images), produce a structured summary as BlockNote JSON blocks. Use a heading for the title and bullets for key points. If the content is minimal or noisy, STILL return a non-empty array with at least one paragraph block containing a concise text summary.")
      };
            foreach (var item in contentItems)
            {
                finalPrompt.Add(item);
            }
            chatHistory.AddUserMessage(finalPrompt);

            var result = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var aiOutput = result?.Content?.ToString() ?? string.Empty;
            var sanitized = SanitizeJsonOutput(aiOutput);
            var parsed = TryParseBlockNoteJsonToObject(sanitized);
            if (parsed is { } obj && obj is List<object> list && list.Count == 0)
            {
                // Avoid empty array; try a minimal text fallback if we had a text payload
                var textFallback = ExtractPlainTextFromAttachment(attachment);
                if (!string.IsNullOrWhiteSpace(textFallback))
                {
                    parsed = BlockNoteDocumentFactory.FromPlainText(textFallback);
                }
            }
            if (parsed is null)
            {
                var textFallback = ExtractPlainTextFromAttachment(attachment);
                parsed = !string.IsNullOrWhiteSpace(textFallback)
                  ? BlockNoteDocumentFactory.FromPlainText(textFallback)
                  : BlockNoteDocumentFactory.FromPlainText($"Summary unavailable for '{attachment.FileName}'.");
            }
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to summarize file via server-side parsing + Gemini. FileName={FileName}, ContentType={ContentType}", attachment.FileName, attachment.ContentType);
            return null;
        }
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
                if (slideIndex > 50) break; // cap to avoid huge prompts
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PPTX parsing failed; returning available content items only.");
        }
        return contentItems;
    }

    // Attempt to strip any accidental markdown fences and extract a clean JSON array
    private static string SanitizeJsonOutput(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var s = input.Trim();

        // Unwrap common object wrappers like { "blocks": [ ... ] }
        if (s.StartsWith("{"))
        {
            var keyIdx = s.IndexOf("\"blocks\"", StringComparison.OrdinalIgnoreCase);
            if (keyIdx < 0) keyIdx = s.IndexOf("blocks", StringComparison.OrdinalIgnoreCase);
            if (keyIdx >= 0)
            {
                var arrStart = s.IndexOf('[', keyIdx);
                if (arrStart >= 0)
                {
                    int depth = 0;
                    for (int i = arrStart; i < s.Length; i++)
                    {
                        var ch = s[i];
                        if (ch == '[')
                        {
                            depth++;
                        }
                        else if (ch == ']')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                var arr = s.Substring(arrStart, i - arrStart + 1).Trim();
                                if (!string.IsNullOrWhiteSpace(arr)) return arr;
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Remove common code fences
        if (s.StartsWith("```"))
        {
            // Try to find the JSON array boundaries inside fenced content
            var start = s.IndexOf('[');
            var end = s.LastIndexOf(']');
            if (start >= 0 && end > start)
            {
                return s.Substring(start, end - start + 1).Trim();
            }
        }

        // If not fenced, still try to extract the array part
        {
            var start = s.IndexOf('[');
            var end = s.LastIndexOf(']');
            if (start >= 0 && end > start)
            {
                return s.Substring(start, end - start + 1).Trim();
            }
        }

        // Fallback: return as-is (caller may validate/parse)
        return s;
    }

    private static object? TryParseBlockNoteJsonToObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var trimmed = json.Trim();
            if (trimmed.StartsWith("````"))
            {
                // unlikely fence variant
                trimmed = trimmed.Trim('`');
            }
            // Strip Markdown code fences if present
            if (trimmed.StartsWith("```"))
            {
                var idx = trimmed.IndexOf('\n');
                if (idx >= 0) trimmed = trimmed[(idx + 1)..];
                if (trimmed.EndsWith("```"))
                {
                    var lastIdx = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                    if (lastIdx >= 0) trimmed = trimmed[..lastIdx];
                }
            }

            var element = JsonSerializer.Deserialize<JsonElement>(trimmed);
            if (element.ValueKind == JsonValueKind.Array)
                return ConvertJsonElement(element);
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
                return ConvertJsonElement(blocks);
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement)
                .ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue)
                ? (object)intValue
                : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static string ExtractPlainTextFromAttachment(AiFileAttachment attachment)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(attachment.ContentType) && attachment.ContentType.StartsWith("text/"))
            {
                using var reader = new StreamReader(attachment.Stream ?? new MemoryStream(attachment.Bytes ?? Array.Empty<byte>()));
                return reader.ReadToEnd();
            }
            if (attachment.Bytes is { Length: > 0 } && string.Equals(attachment.ContentType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.UTF8.GetString(attachment.Bytes);
            }
        }
        catch
        {
            // ignore
        }
        return string.Empty;
    }
}