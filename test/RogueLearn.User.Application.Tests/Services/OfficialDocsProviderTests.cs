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
    public void GetOfficialDocumentationUrl_C_General_Returns_CPPRef_C()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Loops", new List<string>{"c"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("en.cppreference.com/w/c");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Android_RecyclerView_Returns_Android_Docs()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("RecyclerView", new List<string>{"android"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("developer.android.com");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Android_Activity_Returns_Activities_Guide()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Activity lifecycle", new List<string>{"android"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("guide/components/activities");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Android_Layout_Returns_Layout_Guide()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("ConstraintLayout", new List<string>{"android"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("guide/topics/ui/declaring-layout");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Android_Fragment_Returns_Fragment_Guide()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Fragments", new List<string>{"android"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("guide/fragments");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Android_General_Returns_Developer_Guide()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Something else", new List<string>{"android"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("developer.android.com/guide");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_React_General_Returns_React_Learn()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Components", new List<string>{"react"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("react.dev/learn");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Java_General_Returns_Oracle_Java()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Streams", new List<string>{"java"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("docs.oracle.com/en/java/");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_NonTechCategory_Returns_Null()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Vietnam history", new List<string>{"c"}, SubjectCategory.History);
        url.Should().BeNull();
    }

    [Fact]
    public void GetOfficialDocumentationUrl_CPP_Vector_Returns_Container_Url()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("STL vector", new List<string>{"c++"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("en.cppreference.com/w/cpp/container");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_DotNet_Mvc_Returns_Mvc_Url()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("ASP.NET MVC", new List<string>{"c#"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("aspnet/core/mvc");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_DotNet_Api_Returns_Api_Url()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Web API", new List<string>{".net"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("aspnet/core/web-api");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_React_Hooks_Returns_Reference_Url()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("React hook useEffect", new List<string>{"react"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("react.dev/reference/react/hooks");
    }

    [Fact]
    public void GetOfficialDocumentationUrl_Java_Collections_Returns_Collections_Url()
    {
        var url = OfficialDocsProvider.GetOfficialDocumentationUrl("Collections", new List<string>{"java"}, SubjectCategory.Programming);
        url.Should().NotBeNull();
        url!.Should().Contain("docs.oracle.com/javase/tutorial/collections");
    }
}
