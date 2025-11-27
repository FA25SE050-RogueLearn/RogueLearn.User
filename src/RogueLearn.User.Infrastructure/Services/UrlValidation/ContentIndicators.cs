namespace RogueLearn.User.Infrastructure.Services.UrlValidation;

public static class ContentIndicators
{
    public static readonly string[] NotFoundIndicators = new[]
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

    public static readonly string[] PaywallIndicators = new[]
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
}
