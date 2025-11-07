using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Summarization plugin using the configured Semantic Kernel model.
/// For files, it extracts text server-side and then summarizes it.
/// </summary>
public class ContentSummarizationPlugin : ISummarizationPlugin, IFileSummarizationPlugin
{
  private readonly Kernel _kernel;
  private readonly ILogger<ContentSummarizationPlugin> _logger;

  public ContentSummarizationPlugin(Kernel kernel, ILogger<ContentSummarizationPlugin> logger)
  {
    _kernel = kernel;
    _logger = logger;
  }

  public async Task<string> SummarizeTextAsync(string rawText, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
    var prompt = "Summarize the following content in 3-5 bullet points highlighting the key ideas. Be concise and clear.\n\n" + rawText;
    try
    {
      var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
      return (result.GetValue<string>() ?? string.Empty).Trim();
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
      // Final instruction for the AI summarization
      var finalPrompt = new ChatMessageContentItemCollection
      {
        new TextContent("Based on the following content from a document (which may include text and images), please provide a comprehensive summary. Use bullet points and headings where appropriate.")
      };
      foreach (var item in contentItems)
      {
        finalPrompt.Add(item);
      }
      chatHistory.AddUserMessage(finalPrompt);

      var result = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
      var summary = result?.Content?.ToString()?.Trim() ?? string.Empty;
      return summary;
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
}