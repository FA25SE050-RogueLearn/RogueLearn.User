using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using DocumentFormat.OpenXml.Packaging;
using Drawing = DocumentFormat.OpenXml.Drawing;
using System.Text;

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
    "You are a content summarizer that outputs ONLY a valid JSON array of BlockNote blocks. " +
    "Follow these rules strictly: \n" +
    "1) Return ONLY a JSON array (no prose, no markdown fences, no explanations).\n" +
    "2) Use BlockNote built-in blocks: heading, paragraph, bulletListItem, numberedListItem, checkListItem, quote, codeBlock, image, table.\n" +
    "3) Prefer a concise structured summary: a top heading (e.g., 'Summary'), short sections as 'heading', and key points as 'bulletListItem'.\n" +
    "4) Each block is an object with: id (UUID v4), type, props { backgroundColor: 'default', textColor: 'default', textAlignment: 'left' }, content as an array of inline items (e.g., { type: 'text', text: '...', styles: {} }).\n" +
    "5) Keep to <= 20 blocks total. Avoid empty blocks.\n" +
    "6) Do NOT include code fences like ```json or additional commentary.\n" +
    "7) For images found in slides, include image blocks with props { url: '<data-url or source-url>', caption: '...' } only if meaningful; otherwise omit.\n" +
    "8) Ensure the JSON is syntactically valid and can be parsed without modification.";

  public ContentSummarizationPlugin(Kernel kernel, ILogger<ContentSummarizationPlugin> logger)
  {
    _kernel = kernel;
    _logger = logger;
  }

  public async Task<string> SummarizeTextAsync(string rawText, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
    try
    {
      var chatService = _kernel.GetRequiredService<IChatCompletionService>();
      var chatHistory = new ChatHistory();
      chatHistory.AddSystemMessage(BlockNoteJsonInstruction);

      var userItems = new ChatMessageContentItemCollection
      {
        new TextContent("Summarize the following content into BlockNote JSON blocks with clear headings and bullet points:"),
        new TextContent(rawText)
      };
      chatHistory.AddUserMessage(userItems);

      var result = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
      var json = result?.Content?.ToString() ?? string.Empty;
      return SanitizeJsonOutput(json);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to summarize text");
      return string.Empty;
    }
  }

  public async Task<string> SummarizeAsync(AiFileAttachment attachment, CancellationToken cancellationToken = default)
  {
    if (attachment == null) return string.Empty;

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
        if (pptxStream == Stream.Null) return string.Empty;
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
        return string.Empty;
      }

      var chatHistory = new ChatHistory();
      // System instruction to force JSON BlockNote output
      chatHistory.AddSystemMessage(BlockNoteJsonInstruction);

      // Final instruction + content payload for the AI summarization
      var finalPrompt = new ChatMessageContentItemCollection
      {
        new TextContent("Based on the following content from a document (which may include text and images), produce a structured summary as BlockNote JSON blocks. Use a heading for the title and bullets for key points.")
      };
      foreach (var item in contentItems)
      {
        finalPrompt.Add(item);
      }
      chatHistory.AddUserMessage(finalPrompt);

      var result = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
      var json = result?.Content?.ToString() ?? string.Empty;
      return SanitizeJsonOutput(json);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to summarize file via server-side parsing + Gemini. FileName={FileName}, ContentType={ContentType}", attachment.FileName, attachment.ContentType);
      return string.Empty;
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
      _logger.LogWarning(ex, "PPTX parsing failed; will return available content items only.");
    }
    return contentItems;
  }

  // Attempt to strip any accidental markdown fences and extract a clean JSON array
  private static string SanitizeJsonOutput(string input)
  {
    if (string.IsNullOrWhiteSpace(input)) return string.Empty;

    var s = input.Trim();

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
}