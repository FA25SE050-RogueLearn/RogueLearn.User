using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class OfficialDocsProviderTests
{
    [Fact]
    public void GetOfficialDocumentationUrl_C_Pointers_Returns_LearnC_Or_CPPRef()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Pointers and arrays", new List<string>{"c"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        (url!.Contains("learn-c.org") || url.Contains("cppreference.com")).Should().BeTrue();
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Android_RecyclerView_Returns_Android_Docs()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("RecyclerView", new List<string>{"android"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("developer.android.com");
    }
}