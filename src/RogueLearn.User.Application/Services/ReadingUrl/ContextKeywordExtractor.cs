using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Services;

public static class ContextKeywordExtractor
{
    public static List<string> ExtractTechnologyKeywords(string? subjectContext)
    {
        if (string.IsNullOrWhiteSpace(subjectContext))
            return new List<string>();

        var contextLower = subjectContext.ToLowerInvariant();
        var keywords = new List<string>();

        // --- Explicit C Language detection ---
        // Matches: "c language", "c programming", "programming in c"
        // OR isolated "c" surrounded by word boundaries.
        // We aggressively check to exclude "C++" and "C#" contexts from triggering "C".
        if (contextLower.Contains("c language") ||
            contextLower.Contains("c programming") ||
            Regex.IsMatch(contextLower, @"\bc\b"))
        {
            // Safety check: Ensure it's not actually C++ or C# if those words appear immediately next to C
            // (Regex \bc\b usually protects this, but extra caution for "C/C++")
            if (!contextLower.Contains("c++") && !contextLower.Contains("cpp") &&
                !contextLower.Contains("c#") && !contextLower.Contains("sharp"))
            {
                keywords.Add("c");
            }
        }

        // C++ Detection
        if (contextLower.Contains("c++") || contextLower.Contains("cpp"))
            keywords.Add("c++");

        // C# / .NET Detection
        if (contextLower.Contains("c#") || contextLower.Contains("csharp")) keywords.Add("c#");
        if (contextLower.Contains(".net") && !contextLower.Contains("dotnet.vn")) keywords.Add(".net");
        if (contextLower.Contains("asp.net") || contextLower.Contains("aspnet")) keywords.Add("asp.net");

        // Java Detection
        // Ensure we don't accidentally pick up "javascript"
        if (contextLower.Contains("java") && !contextLower.Contains("javascript"))
        {
            keywords.Add("java");
            if (contextLower.Contains("servlet") || contextLower.Contains("jsp"))
                keywords.Add("java-web");
            if (contextLower.Contains("spring"))
                keywords.Add("spring");
        }

        // Web Detection
        if (contextLower.Contains("node")) keywords.Add("nodejs");
        if (contextLower.Contains("express")) keywords.Add("express");
        if (contextLower.Contains("react")) keywords.Add("react");
        if (contextLower.Contains("vue")) keywords.Add("vue");
        if (contextLower.Contains("angular")) keywords.Add("angular");
        if (contextLower.Contains("javascript") && !contextLower.Contains("java ")) keywords.Add("javascript");
        if (contextLower.Contains("typescript")) keywords.Add("typescript");

        // Mobile / Other
        if (contextLower.Contains("android")) keywords.Add("android");
        if (contextLower.Contains("mobile")) keywords.Add("mobile");
        if (contextLower.Contains("kotlin")) keywords.Add("kotlin");
        if (contextLower.Contains("flutter")) keywords.Add("flutter");
        if (contextLower.Contains("ios") || contextLower.Contains("swift")) keywords.Add("ios");

        // Database
        if (contextLower.Contains("sql")) keywords.Add("sql");
        if (contextLower.Contains("database")) keywords.Add("database");
        if (contextLower.Contains("python")) keywords.Add("python");

        return keywords.Distinct().ToList();
    }
}