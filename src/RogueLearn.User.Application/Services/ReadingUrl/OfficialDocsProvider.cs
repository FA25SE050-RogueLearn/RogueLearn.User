// RogueLearn.User/src/RogueLearn.User.Application/Services/ReadingUrl/OfficialDocsProvider.cs
namespace RogueLearn.User.Application.Services;

public static class OfficialDocsProvider
{
    public static string? GetOfficialDocumentationUrl(string topic, List<string> technologyKeywords, SubjectCategory category)
    {
        var topicLower = topic.ToLowerInvariant();

        if (category != SubjectCategory.Programming && category != SubjectCategory.ComputerScience)
            return null;

        // C Reference
        if (technologyKeywords.Contains("c"))
        {
            if (topicLower.Contains("pointer") || topicLower.Contains("array"))
                return "https://www.learn-c.org/"; // Good interactive tutorial
            return "https://en.cppreference.com/w/c"; // The definitive C reference
        }

        // C++ Reference
        if (technologyKeywords.Contains("c++"))
        {
            if (topicLower.Contains("stl") || topicLower.Contains("vector"))
                return "https://en.cppreference.com/w/cpp/container";
            return "https://en.cppreference.com/w/cpp";
        }

        // Android
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

        // .NET / C#
        if (technologyKeywords.Contains("asp.net") || technologyKeywords.Contains("c#") || technologyKeywords.Contains(".net"))
        {
            if (topicLower.Contains("mvc"))
                return "https://learn.microsoft.com/en-us/aspnet/core/mvc/overview";
            if (topicLower.Contains("api"))
                return "https://learn.microsoft.com/en-us/aspnet/core/web-api";
            return "https://learn.microsoft.com/en-us/dotnet/csharp/";
        }

        // React
        if (technologyKeywords.Contains("react"))
        {
            if (topicLower.Contains("hook"))
                return "https://react.dev/reference/react/hooks";
            return "https://react.dev/learn";
        }

        // Java
        if (technologyKeywords.Contains("java"))
        {
            if (topicLower.Contains("collection") || topicLower.Contains("list"))
                return "https://docs.oracle.com/javase/tutorial/collections/index.html";
            return "https://docs.oracle.com/en/java/";
        }

        return null;
    }
}