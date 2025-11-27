using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Services;

public static class TopicNormalizer
{
    private static readonly string[] MetaTokens = new[]
    {
        "review",
        "progress test",
        "midterm",
        "final exam",
        "quiz",
        "exercise",
        "assignment"
    };

    public static string Normalize(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return string.Empty;

        var t = topic.Trim();

        t = Regex.Replace(t, @"\s+", " ");

        var segments = t.Split(new[] { '.', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();

        var cleanedSegments = new List<string>();
        foreach (var s in segments)
        {
            var cleaned = Regex.Replace(s, @"^\s*\d+(\.\d+)*\s*", string.Empty);
            cleaned = Regex.Replace(cleaned, @"^\s*(section|chapter)\s*\d+\s*", string.Empty, RegexOptions.IgnoreCase);
            cleaned = cleaned.Trim();
            if (!string.IsNullOrEmpty(cleaned)) cleanedSegments.Add(cleaned);
        }

        var normalized = string.Join(" ", cleanedSegments);
        normalized = Regex.Replace(normalized, @"[^\p{L}\p{N}\s]", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    public static bool IsMetaSession(string topic)
    {
        var tl = (topic ?? string.Empty).ToLowerInvariant();
        return MetaTokens.Any(m => tl.Contains(m));
    }
}
