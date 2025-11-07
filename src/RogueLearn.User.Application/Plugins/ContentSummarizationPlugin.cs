using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Interfaces;

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

    // Obtain bytes from either Bytes or Stream (preferred), without forcing upstream to materialize large arrays.
    byte[]? bytes = attachment.Bytes;
    if (bytes == null || bytes.Length == 0)
    {
      if (attachment.Stream is null)
      {
        return string.Empty;
      }
      try
      {
        using var ms = new MemoryStream();
        await attachment.Stream.CopyToAsync(ms, cancellationToken);
        bytes = ms.ToArray();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to read attachment stream for summarization. FileName={FileName}", attachment.FileName);
        return string.Empty;
      }
    }

    // Fallback: send the raw file bytes to Gemini via Semantic Kernel's multimodal chat API.
    // This works for images and, in many cases, for PDFs and other document MIME types supported by the provider.
    try
    {
      var chatService = _kernel.GetRequiredService<IChatCompletionService>();

      var systemPrompt = "You are a helpful assistant. Your task is to read the attached file and summarize it focusing on the main ideas, key points, and any important details. The results should be presented in a clear and concise manner with headings, bullet points, and any necessary subheadings. Be sure to include any relevant examples, quotes, or numbers that support the main ideas.";
      var chatHistory = new ChatHistory(systemPrompt);

      var items = new ChatMessageContentItemCollection
            {
                new TextContent("Please summarize the attached file."),
                // Pass the bytes with the original MIME type (e.g., image/png, application/pdf, etc.)
                new ImageContent(new ReadOnlyMemory<byte>(bytes), attachment.ContentType)
            };

      chatHistory.AddUserMessage(items);

      var reply = await chatService.GetChatMessageContentAsync(chatHistory);
      var summary = reply?.Content?.ToString()?.Trim() ?? string.Empty;
      return summary;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to summarize file via Gemini chat completion. FileName={FileName}, ContentType={ContentType}", attachment.FileName, attachment.ContentType);
      return string.Empty;
    }
  }
}