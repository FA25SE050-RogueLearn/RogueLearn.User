namespace RogueLearn.User.Infrastructure.Services.UrlValidation;

public static class UrlHostUtils
{
    public static bool IsTrustedDomain(string url, HashSet<string> trustedDomains)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        return trustedDomains.Any(domain => host == domain || host.EndsWith($".{domain}"));
    }
}
