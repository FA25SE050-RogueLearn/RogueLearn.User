// RogueLearn.User/src/RogueLearn.User.Infrastructure/Services/UrlValidationService.cs
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

/// <summary>
/// Validates URLs for accessibility and detects soft 404s, paywalls, and restricted content.
/// Uses trusted domain whitelist to skip content validation for JavaScript-rendered sites.
/// </summary>
public class UrlValidationService : IUrlValidationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UrlValidationService> _logger;

    // Trusted domains that skip content length validation
    // These are well-known educational/tutorial sites that may use JS rendering
    private static readonly HashSet<string> _trustedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Programming Tutorials - Vietnamese
        "viblo.asia",
        "topdev.vn",
        "200lab.io",
        "techtalk.vn",
        "techmaster.vn",
        
        // Programming Tutorials - International
        "geeksforgeeks.org",
        "w3schools.com",
        "tutorialspoint.com",
        "programiz.com",
        "javatpoint.com",
        "tutorialsteacher.com",
        "guru99.com",
        "studytonight.com",
        "baeldung.com",         // Java tutorials
        "jenkov.com",           // Java tutorials
        "mkyong.com",           // Java/Spring tutorials
        
        // Developer Blogs
        "dev.to",
        "hashnode.dev",
        "freecodecamp.org",
        "digitalocean.com",
        "css-tricks.com",
        "smashingmagazine.com",
        "logrocket.com",
        "sitepoint.com",
        
        // Official Documentation
        "developer.android.com",
        "learn.microsoft.com",
        "docs.microsoft.com",
        "react.dev",
        "reactjs.org",
        "vuejs.org",
        "angular.io",
        "docs.oracle.com",
        "oracle.com",
        "developer.mozilla.org",
        "nodejs.org",
        "docs.python.org",
        "kotlinlang.org",
        "dart.dev",
        "flutter.dev",
        
        // Vietnamese Educational Sites
        "vietjack.com",
        "tailieu.vn",
        "123doc.net",
        "hocmai.vn",
        "tuyensinh247.com",
        "loigiaihay.com",
        
        // Vietnamese News/Politics (for non-programming subjects)
        "thuvienphapluat.vn",
        "dangcongsan.vn",
        "chinhphu.vn",
        "nhandan.vn",
        "vnexpress.net",
        "thanhnien.vn",
        "tuoitre.vn",
        "dantri.com.vn",
        "cafef.vn",
        "baomoi.com",
        
        // Academic Sources
        "wikipedia.org",
        "britannica.com",
        "khanacademy.org",
        "coursera.org",
        "edx.org",
        "mit.edu",
        "stanford.edu",
        "berkeley.edu"
    };

    // Patterns indicating unavailable content
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

    // Patterns indicating paywalls or login requirements
    private static readonly string[] _paywallIndicators = new[]
    {
        "member-only",
        "members only",
        "subscribe to read",
        "subscription required",
        "sign in to continue",
        "login to continue",
        "this article is for",
        "premium content",
        "become a member",
        "upgrade to read",
        "limited free article",
        "free articles remaining",
        "paywall",
        "unlock this story"
    };

    public UrlValidationService(IHttpClientFactory httpClientFactory, ILogger<UrlValidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a URL is from a trusted domain that can skip content validation.
    /// Supports both exact match and subdomain match.
    /// </summary>
    private bool IsTrustedDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();

        // Check if host matches any trusted domain
        // e.g., "developer.android.com" matches "android.com"
        return _trustedDomains.Any(domain =>
            host == domain || host.EndsWith($".{domain}"));
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

            // Try HEAD request first
            var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            headRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            HttpResponseMessage? headResponse = null;
            try
            {
                headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if ((int)headResponse.StatusCode >= 400)
                {
                    _logger.LogWarning("URL validation failed for '{Url}'. Received status code {StatusCode}.",
                        url, (int)headResponse.StatusCode);
                    return false;
                }
            }
            catch (HttpRequestException)
            {
                _logger.LogDebug("HEAD request failed for '{Url}', falling back to GET.", url);
            }
            finally
            {
                headResponse?.Dispose();
            }

            // Fetch actual content
            var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
            getRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            getRequest.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            getRequest.Headers.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");

            var response = await client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("URL validation failed for '{Url}'. Received status code {StatusCode}.",
                    url, (int)response.StatusCode);
                return false;
            }

            // Skip detailed validation for non-HTML content
            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (contentType != null && !contentType.Contains("html") && !contentType.Contains("text"))
            {
                _logger.LogInformation("URL validation successful for '{Url}' (non-HTML content: {ContentType}).",
                    url, contentType);
                return true;
            }

            // ✅ NEW: Skip content validation for trusted domains
            // These sites are vetted and may use JavaScript rendering
            if (IsTrustedDomain(url))
            {
                _logger.LogInformation("✅ URL validation successful for '{Url}' (trusted domain - skipping content check).",
                    url);
                return true;
            }

            // Read content to check for soft 404s and paywalls (untrusted domains only)
            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(contentStream);

            // Read first 100KB to catch paywall overlays and modals
            var buffer = new char[102400];
            var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
            var contentSnippet = new string(buffer, 0, charsRead);

            // Log HTML preview for debugging (only in Debug mode)
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var preview = contentSnippet.Length > 500
                    ? contentSnippet.Substring(0, 500) + "..."
                    : contentSnippet;
                _logger.LogDebug("HTML Preview for '{Url}': {Preview}", url, preview);
            }

            var contentLower = contentSnippet.ToLowerInvariant();

            // Check for "not found" indicators
            foreach (var indicator in _notFoundIndicators)
            {
                if (contentLower.Contains(indicator))
                {
                    _logger.LogWarning("URL '{Url}' appears to be a soft 404 (contains '{Indicator}').",
                        url, indicator);
                    return false;
                }
            }

            // Check for paywall indicators
            foreach (var indicator in _paywallIndicators)
            {
                if (contentLower.Contains(indicator))
                {
                    _logger.LogWarning("URL '{Url}' appears to have a paywall (contains '{Indicator}').",
                        url, indicator);
                    return false;
                }
            }

            // Check for minimal content (possible blank page or redirect stub)
            var meaningfulText = ExtractTextFromHtml(contentSnippet);

            if (meaningfulText.Length < 200)
            {
                _logger.LogWarning(
                    "URL '{Url}' has insufficient content (only {Length} characters of text). " +
                    "Extracted text preview: '{TextPreview}'",
                    url,
                    meaningfulText.Length,
                    meaningfulText.Length > 100 ? meaningfulText.Substring(0, 100) + "..." : meaningfulText);
                return false;
            }

            _logger.LogInformation("URL validation successful for '{Url}' with status code {StatusCode}.",
                url, (int)response.StatusCode);
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

    /// <summary>
    /// Extracts meaningful text content from HTML, handling common JS-rendering patterns.
    /// Properly removes script/style blocks and decodes HTML entities.
    /// </summary>
    private string ExtractTextFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        try
        {
            // Remove script and style tags entirely (with their content)
            var withoutScripts = Regex.Replace(html,
                @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var withoutStyles = Regex.Replace(withoutScripts,
                @"<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove HTML comments
            var withoutComments = Regex.Replace(withoutStyles,
                @"<!--.*?-->",
                string.Empty,
                RegexOptions.Singleline);

            // Remove all remaining HTML tags
            var textContent = Regex.Replace(withoutComments, @"<[^>]+>", string.Empty);

            // Decode common HTML entities
            textContent = System.Net.WebUtility.HtmlDecode(textContent);

            // Normalize whitespace (collapse multiple spaces/newlines into single space)
            textContent = Regex.Replace(textContent, @"\s+", " ");

            return textContent.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from HTML");
            return string.Empty;
        }
    }
}