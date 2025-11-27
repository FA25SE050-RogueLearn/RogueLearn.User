namespace RogueLearn.User.Application.Services;

public static class OfficialDocsProvider
{
    public static string? GetOfficialDocumentationUrl(string topic, List<string> technologyKeywords, SubjectCategory category)
    {
        var topicLower = topic.ToLowerInvariant();

        if (category != SubjectCategory.Programming && category != SubjectCategory.ComputerScience)
            return null;

        if (technologyKeywords.Contains("android"))
        {
            if (topicLower.Contains("activity"))
                return "https://developer.android.com/guide/components/activities";
            if (topicLower.Contains("layout"))
                return "https://developer.android.com/guide/topics/ui/declaring-layout";
            if (topicLower.Contains("recyclerview"))
                return "https://developer.android.com/develop/ui/views/layout/recyclerview";
            if (topicLower.Contains("fragment"))
                return "https://developer.android.com/guide/fragments";

            return "https://developer.android.com/guide";
        }

        if (technologyKeywords.Contains("asp.net") || technologyKeywords.Contains("c#"))
        {
            if (topicLower.Contains("mvc"))
                return "https://learn.microsoft.com/en-us/aspnet/core/mvc/overview";

            return "https://learn.microsoft.com/en-us/aspnet/core/";
        }

        if (technologyKeywords.Contains("react"))
        {
            if (topicLower.Contains("hook"))
                return "https://react.dev/reference/react/hooks";

            return "https://react.dev/learn";
        }

        return null;
    }
}
