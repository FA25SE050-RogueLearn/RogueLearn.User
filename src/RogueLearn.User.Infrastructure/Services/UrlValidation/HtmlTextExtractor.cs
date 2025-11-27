using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Infrastructure.Services.UrlValidation;

public static class HtmlTextExtractor
{
    public static string Extract(string html, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        try
        {
            var withoutScripts = Regex.Replace(html,
                @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var withoutStyles = Regex.Replace(withoutScripts,
                @"<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var withoutComments = Regex.Replace(withoutStyles,
                @"<!--.*?-->",
                string.Empty,
                RegexOptions.Singleline);

            var textContent = Regex.Replace(withoutComments, @"<[^>]+>", string.Empty);
            textContent = System.Net.WebUtility.HtmlDecode(textContent);
            textContent = Regex.Replace(textContent, @"\s+", " ");
            return textContent.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting text from HTML");
            return string.Empty;
        }
    }
}
