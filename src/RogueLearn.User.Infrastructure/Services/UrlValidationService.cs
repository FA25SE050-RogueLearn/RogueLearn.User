// RogueLearn.User/src/RogueLearn.User.Infrastructure/Services/UrlValidationService.cs
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

/// <summary>
/// Implements URL validation using HTTP requests with content checking to detect soft 404s.
/// </summary>
public class UrlValidationService : IUrlValidationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UrlValidationService> _logger;

    // Common patterns that indicate a page doesn't exist or has no content
    private static readonly string[] _notFoundIndicators = new[]
    {
        "page not found",
        "404",
        "page does not exist",
        "page has been removed",
        "page has moved",
        "no longer available",
        "content not found",
        "this page doesn't exist",
        "page cannot be found",
        "requested page",
        "error 404",
        "not exist",
        "we couldn't find that page",
        "the page you requested"
    };

    public UrlValidationService(IHttpClientFactory httpClientFactory, ILogger<UrlValidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> IsUrlAccessibleAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            _logger.LogWarning("URL validation failed: Invalid or empty URL.");
            return false;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Try HEAD request first for efficiency
            var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            headRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            HttpResponseMessage? headResponse = null;
            try
            {
                headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // If HEAD returns a clear error status, fail fast
                if ((int)headResponse.StatusCode >= 400)
                {
                    _logger.LogWarning("URL validation failed for '{Url}'. Received status code {StatusCode}.", url, (int)headResponse.StatusCode);
                    return false;
                }
            }
            catch (HttpRequestException)
            {
                // HEAD request failed, will try GET below
                _logger.LogDebug("HEAD request failed for '{Url}', falling back to GET.", url);
            }
            finally
            {
                headResponse?.Dispose();
            }

            // Fetch actual content to check for soft 404s
            var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
            getRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("URL validation failed for '{Url}'. Received status code {StatusCode}.", url, (int)response.StatusCode);
                return false;
            }

            // Check if content type is HTML (skip detailed validation for PDFs, images, etc.)
            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (contentType != null && !contentType.Contains("html") && !contentType.Contains("text"))
            {
                _logger.LogInformation("URL validation successful for '{Url}' (non-HTML content: {ContentType}).", url, contentType);
                return true;
            }

            // Read a portion of the HTML content to check for "soft 404s"
            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(contentStream);

            // Read first 50KB of content (enough to detect 404 pages without loading entire page)
            var buffer = new char[51200];
            var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
            var contentSnippet = new string(buffer, 0, charsRead);
            var contentLower = contentSnippet.ToLowerInvariant();

            // Check for common "not found" indicators in the content
            foreach (var indicator in _notFoundIndicators)
            {
                if (contentLower.Contains(indicator))
                {
                    _logger.LogWarning("URL '{Url}' appears to be a soft 404 (contains '{Indicator}').", url, indicator);
                    return false;
                }
            }

            // Additional heuristic: Check if the page has minimal content
            // Remove HTML tags and check actual text length
            var textContent = Regex.Replace(contentSnippet, "<.*?>", string.Empty);
            var meaningfulText = Regex.Replace(textContent, @"\s+", " ").Trim();

            if (meaningfulText.Length < 100)
            {
                _logger.LogWarning("URL '{Url}' has insufficient content (only {Length} characters of text).", url, meaningfulText.Length);
                return false;
            }

            _logger.LogInformation("URL validation successful for '{Url}' with status code {StatusCode}.", url, (int)response.StatusCode);
            return true;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("URL validation timed out for '{Url}'.", url);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception occurred while validating URL '{Url}'.", url);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred while validating URL '{Url}'.", url);
            return false;
        }
    }
}