namespace RogueLearn.User.Application.Services;

public static class SearchQueryBuilder
{
    public static string BuildContextAwareQuery(string topic, string? subjectContext, SubjectCategory category)
    {
        switch (category)
        {
            case SubjectCategory.Programming:
                if (!string.IsNullOrWhiteSpace(subjectContext))
                {
                    var contextTokens = subjectContext.Split(new[] { ',', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).Take(3);
                    return $"{string.Join(" ", contextTokens)} {topic} tutorial";
                }
                return $"{topic} tutorial guide";

            case SubjectCategory.ComputerScience:
                return $"{topic} guide tutorial explanation";

            case SubjectCategory.VietnamesePolitics:
                return $"{topic} lý thuyết bài giảng";

            case SubjectCategory.History:
                return $"{topic} tài liệu lịch sử";

            case SubjectCategory.VietnameseLiterature:
                return $"{topic} bài tập trắc nghiệm";

            case SubjectCategory.Science:
                return $"{topic} lý thuyết công thức";

            case SubjectCategory.Business:
                return $"{topic} bài giảng kinh tế";

            default:
                bool isVietnamese = topic.Contains(" và ") ||
                                   topic.Contains(" của ") ||
                                   topic.Contains(" là ") ||
                                   topic.Contains(" được ") ||
                                   topic.Contains(" trong ");

                if (isVietnamese)
                {
                    return $"{topic} tài liệu học tập";
                }
                else
                {
                    return $"{topic} guide tutorial explanation";
                }
        }
    }

    public static List<string> BuildQueryVariants(string topic, string? subjectContext, SubjectCategory category)
    {
        var variants = new List<string>();

        var baseTopic = topic?.Trim() ?? string.Empty;
        var ctx = subjectContext?.Trim() ?? string.Empty;

        bool containsVietnameseDiacritics = System.Text.RegularExpressions.Regex.IsMatch(baseTopic + ctx, @"[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđĐ]");

        var (enCore, viCore) = ExtractCoreConcepts(baseTopic);

        switch (category)
        {
            case SubjectCategory.Programming:
                var ctxTokens = string.IsNullOrWhiteSpace(ctx)
                    ? Array.Empty<string>()
                    : ctx.Split(new[] { ',', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).Take(3).ToArray();
                var english = $"{string.Join(" ", ctxTokens)} {enCore} tutorial guide".Trim();
                variants.Add(english);
                if (containsVietnameseDiacritics)
                {
                    variants.Add($"{viCore} hướng dẫn lập trình ví dụ");
                }
                break;

            case SubjectCategory.ComputerScience:
                // Software Engineering / Requirements Engineering supportive queries
                if (IsRequirementsTopic(baseTopic))
                {
                    variants.Add($"{enCore} software requirements engineering guide tutorial");
                    variants.Add($"{enCore} requirements elicitation techniques tutorial");
                    variants.Add($"{enCore} requirements validation checklist guide");
                    variants.Add($"{enCore} use case template example tutorial");
                    if (containsVietnameseDiacritics)
                    {
                        variants.Add($"{viCore} kỹ thuật thu thập yêu cầu hướng dẫn");
                        variants.Add($"{viCore} xác thực yêu cầu phần mềm bài viết");
                        variants.Add($"{viCore} mẫu use case ví dụ");
                        variants.Add($"{viCore} tiêu chí chấp nhận hướng dẫn");
                    }
                }
                else
                {
                    variants.Add($"{enCore} guide tutorial explanation");
                    if (containsVietnameseDiacritics)
                        variants.Add($"{viCore} bài giảng lý thuyết giải thích");
                }
                if (containsVietnameseDiacritics)
                {
                    // Extra Vietnamese study phrasing
                    variants.Add($"{viCore} tài liệu học tập");
                }
                break;

            case SubjectCategory.VietnamesePolitics:
            case SubjectCategory.History:
            case SubjectCategory.VietnameseLiterature:
            case SubjectCategory.Science:
            case SubjectCategory.Business:
                variants.Add(containsVietnameseDiacritics ? $"{viCore} tài liệu bài giảng" : $"{enCore} study materials lecture notes");
                break;

            default:
                variants.Add(containsVietnameseDiacritics ? $"{viCore} tài liệu học tập hướng dẫn" : $"{enCore} guide tutorial explanation");
                break;
        }

        // Ensure distinct and non-empty
        return variants.Where(v => !string.IsNullOrWhiteSpace(v))
                       .Select(v => v.Trim())
                       .Distinct()
                       .ToList();
    }

    private static bool IsRequirementsTopic(string topic)
    {
        var t = topic.ToLowerInvariant();
        var keys = new[]
        {
            "requirement", "requirements", "elicitation", "validation", "verification",
            "use case", "vision and scope", "acceptance criteria", "prioritization", "reuse"
        };
        return keys.Any(k => t.Contains(k));
    }

    private static (string enCore, string viCore) ExtractCoreConcepts(string topic)
    {
        var t = topic.ToLowerInvariant();
        // Simple normalization: remove punctuation, parentheses, connectors
        var cleaned = new string(t.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
        var tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

        // Map well-known phrases to concise cores
        var en = "";
        var vi = "";

        if (tokens.Contains("requirements") && tokens.Contains("engineering"))
        {
            en = "software requirements engineering";
            vi = "kỹ thuật yêu cầu phần mềm";
        }
        else if (tokens.Contains("elicitation"))
        {
            en = "requirements elicitation";
            vi = "thu thập yêu cầu";
        }
        else if (tokens.Contains("validation") || tokens.Contains("verification"))
        {
            en = "requirements validation";
            vi = "xác thực yêu cầu";
        }
        else if (tokens.Contains("use") && tokens.Contains("case"))
        {
            en = "use case";
            vi = "trường hợp sử dụng";
        }
        else if (tokens.Contains("acceptance") && tokens.Contains("criteria"))
        {
            en = "acceptance criteria";
            vi = "tiêu chí chấp nhận";
        }
        else if (tokens.Contains("vision") && tokens.Contains("scope"))
        {
            en = "vision and scope";
            vi = "tầm nhìn và phạm vi";
        }
        else if (tokens.Contains("prioritization") || tokens.Contains("priorities") || tokens.Contains("prioritize"))
        {
            en = "requirements prioritization";
            vi = "ưu tiên yêu cầu";
        }
        else if (tokens.Contains("reuse"))
        {
            en = "requirements reuse";
            vi = "tái sử dụng yêu cầu";
        }
        else if (tokens.Contains("business") && tokens.Contains("analyst"))
        {
            en = "business analyst";
            vi = "chuyên viên phân tích kinh doanh";
        }
        else
        {
            // Fallback: keep short first 5 tokens
            en = string.Join(' ', tokens.Take(5));
            vi = en; // If we cannot map, use the same core
        }

        return (en, vi);
    }
}
