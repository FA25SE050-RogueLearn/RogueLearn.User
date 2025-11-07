using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using System.Text.Json;

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

        // Acquire bytes from either Bytes or Stream.
        byte[]? bytes = attachment.Bytes;
        if (bytes == null || bytes.Length == 0)
        {
            if (attachment.Stream is null)
            {
                return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
            }
            try
            {
                using var ms = new MemoryStream();
                await attachment.Stream.CopyToAsync(ms, cancellationToken);
                bytes = ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read attachment stream for tag suggestion. FileName={FileName}", attachment.FileName);
                return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
            }
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

            var items = new ChatMessageContentItemCollection
            {
                new TextContent(instructions),
                new ImageContent(new ReadOnlyMemory<byte>(bytes), attachment.ContentType)
            };
            chatHistory.AddUserMessage(items);

            var reply = await chatService.GetChatMessageContentAsync(chatHistory);
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
}