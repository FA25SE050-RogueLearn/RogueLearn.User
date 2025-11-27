namespace RogueLearn.User.Application.Services;

public static class ContextKeywordExtractor
{
    public static List<string> ExtractTechnologyKeywords(string? subjectContext)
    {
        if (string.IsNullOrWhiteSpace(subjectContext))
            return new List<string>();

        var contextLower = subjectContext.ToLowerInvariant();
        var keywords = new List<string>();

        if (contextLower.Contains("android")) keywords.Add("android");
        if (contextLower.Contains("mobile")) keywords.Add("mobile");
        if (contextLower.Contains("kotlin")) keywords.Add("kotlin");

        if (contextLower.Contains("java") && !contextLower.Contains("javascript"))
        {
            keywords.Add("java");
            if (contextLower.Contains("servlet") || contextLower.Contains("jsp"))
                keywords.Add("java-web");
            if (contextLower.Contains("spring"))
                keywords.Add("spring");
        }

        if (contextLower.Contains("asp.net") || contextLower.Contains("aspnet")) keywords.Add("asp.net");
        if (contextLower.Contains("c#") || contextLower.Contains("csharp")) keywords.Add("c#");
        if (contextLower.Contains(".net") && !contextLower.Contains("dotnet.vn")) keywords.Add(".net");

        if (contextLower.Contains("node")) keywords.Add("nodejs");
        if (contextLower.Contains("express")) keywords.Add("express");

        if (contextLower.Contains("react")) keywords.Add("react");
        if (contextLower.Contains("vue")) keywords.Add("vue");
        if (contextLower.Contains("angular")) keywords.Add("angular");
        if (contextLower.Contains("javascript") && !contextLower.Contains("java ")) keywords.Add("javascript");
        if (contextLower.Contains("typescript")) keywords.Add("typescript");

        if (contextLower.Contains("python")) keywords.Add("python");
        if (contextLower.Contains("flutter")) keywords.Add("flutter");
        if (contextLower.Contains("ios") || contextLower.Contains("swift")) keywords.Add("ios");

        return keywords.Distinct().ToList();
    }
}
