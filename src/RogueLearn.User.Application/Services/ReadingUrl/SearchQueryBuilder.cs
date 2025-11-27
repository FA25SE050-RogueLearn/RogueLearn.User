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
}
