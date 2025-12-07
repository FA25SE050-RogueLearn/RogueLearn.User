using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class SearchResultFilterTests
{
    [Fact]
    public void IsWrongFramework_Blocks_Python_In_C_Context()
    {
        var url = "https://docs.python.org/3/tutorial/";
        var tech = new List<string> { "c" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "pointers", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Blocks_Wrong_W3Schools_Section_For_C()
    {
        var url = "https://www.w3schools.com/java/";
        var tech = new List<string> { "c" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Blocks_Oracle_In_DotNet_Context()
    {
        var url = "https://docs.oracle.com/javase/tutorial/";
        var tech = new List<string> { "c#", ".net" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "ASP.NET MVC", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Java_Blocks_DotNet_Paths()
    {
        var url = "https://learn.microsoft.com/en-us/dotnet/csharp/";
        var tech = new List<string> { "java" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "servlets", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsUntrustedSource_Blocks_Pdf_And_Scribd()
    {
        var logger = Substitute.For<ILogger>();
        var b1 = SearchResultFilter.IsUntrustedSource("https://example.com/file.pdf", SubjectCategory.Programming, logger);
        var b2 = SearchResultFilter.IsUntrustedSource("https://scribd.com/doc/1", SubjectCategory.Programming, logger);
        b1.Should().BeTrue();
        b2.Should().BeTrue();
    }

    [Fact]
    public void IsUntrustedSource_Blocks_Medium_In_Programming()
    {
        var logger = Substitute.For<ILogger>();
        var blocked = SearchResultFilter.IsUntrustedSource("https://medium.com/some-post", SubjectCategory.Programming, logger);
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsUntrustedSource_Allows_Vietnamese_News_In_Literature()
    {
        var logger = Substitute.For<ILogger>();
        var allowed = SearchResultFilter.IsUntrustedSource("https://vnexpress.net/some-article", SubjectCategory.VietnameseLiterature, logger);
        allowed.Should().BeFalse();
    }

    [Fact]
    public void IsUntrustedSource_Blocks_StackOverflow_Questions()
    {
        var logger = Substitute.For<ILogger>();
        var blocked = SearchResultFilter.IsUntrustedSource("https://stackoverflow.com/questions/1234/some-question", SubjectCategory.Programming, logger);
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsUntrustedSource_Blocks_Reddit_And_YouTube()
    {
        var logger = Substitute.For<ILogger>();
        SearchResultFilter.IsUntrustedSource("https://reddit.com/r/programming", SubjectCategory.Programming, logger).Should().BeTrue();
        SearchResultFilter.IsUntrustedSource("https://youtube.com/watch?v=abc", SubjectCategory.Programming, logger).Should().BeTrue();
    }

    [Fact]
    public void IsUntrustedSource_Programming_Allows_TutorialSites()
    {
        var logger = Substitute.For<ILogger>();
        SearchResultFilter.IsUntrustedSource("https://www.geeksforgeeks.org/c-programming-language/", SubjectCategory.Programming, logger).Should().BeFalse();
    }

    [Fact]
    public void IsUntrustedSource_Blocks_Zip_Files()
    {
        var logger = Substitute.For<ILogger>();
        var blocked = SearchResultFilter.IsUntrustedSource("https://example.com/archive.zip", SubjectCategory.Programming, logger);
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsUntrustedSource_Universal_Blocks_Docs_And_Slides()
    {
        var logger = Substitute.For<ILogger>();
        SearchResultFilter.IsUntrustedSource("https://example.com/whitepaper.docx", SubjectCategory.Programming, logger).Should().BeTrue();
        SearchResultFilter.IsUntrustedSource("https://slideshare.net/some-slide", SubjectCategory.Programming, logger).Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Returns_False_Outside_Programming_CS()
    {
        var url = "https://react.dev/learn";
        var tech = new List<string> { "react" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Business, "components", Substitute.For<ILogger>());
        isWrong.Should().BeFalse();
    }

    [Fact]
    public void IsWrongFramework_Java_Blocks_JsPaths()
    {
        var url = "https://example.com/javascript/tutorial";
        var tech = new List<string> { "java" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "servlets", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Java_Blocks_W3Schools_NonJava_Section()
    {
        var url = "https://www.w3schools.com/cs/";
        var tech = new List<string> { "java" };
        var blocked = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "servlets", Substitute.For<ILogger>());
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Java_Blocks_DotNet_Paths_All()
    {
        var tech = new List<string> { "java" };
        var baseUrl = "https://example.com";
        var paths = new[] { "/asp/", "/aspnet/", "/dotnet/", "/csharp/", "/cs/", "/asp.net/", "/c-sharp/", "/vb.net/" };
        foreach (var p in paths)
        {
            SearchResultFilter.IsWrongFramework(baseUrl + p + "intro", tech, SubjectCategory.Programming, "servlets", Substitute.For<ILogger>()).Should().BeTrue();
        }
    }

    [Fact]
    public void IsWrongFramework_Cpp_Blocks_Python_Sites()
    {
        var tech = new List<string> { "c++" };
        SearchResultFilter.IsWrongFramework("https://docs.python.org/3/tutorial/", tech, SubjectCategory.Programming, "vectors", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://pypi.org/project/requests/", tech, SubjectCategory.Programming, "vectors", Substitute.For<ILogger>()).Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Cpp_Blocks_Py_Path_Shorthand()
    {
        var tech = new List<string> { "c++" };
        var blocked = SearchResultFilter.IsWrongFramework("https://example.com/py/guide", tech, SubjectCategory.Programming, "vectors", Substitute.For<ILogger>());
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_C_Blocks_DotNet_Learn_Site()
    {
        var tech = new List<string> { "c" };
        SearchResultFilter.IsWrongFramework("https://learn.microsoft.com/en-us/dotnet/csharp/", tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>()).Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Cpp_Blocks_Baeldung_Java_Site()
    {
        var tech = new List<string> { "c++" };
        var blocked = SearchResultFilter.IsWrongFramework("https://www.baeldung.com/java-collections", tech, SubjectCategory.Programming, "vectors", Substitute.For<ILogger>());
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_C_Blocks_Js_Web_Paths()
    {
        var tech = new List<string> { "c" };
        SearchResultFilter.IsWrongFramework("https://example.com/js/reference", tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/react/learn", tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/angular/components", tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/vue/guide", tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>()).Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Python_Blocks_OtherLangs_Paths()
    {
        var tech = new List<string> { "python" };
        SearchResultFilter.IsWrongFramework("https://example.com/java/intro", tech, SubjectCategory.Programming, "tuples", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/csharp/reference", tech, SubjectCategory.Programming, "tuples", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/cpp/guide", tech, SubjectCategory.Programming, "tuples", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/golang/intro", tech, SubjectCategory.Programming, "tuples", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/ruby/intro", tech, SubjectCategory.Programming, "tuples", Substitute.For<ILogger>()).Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Cpp_W3Schools_Wrong_Section_Blocks()
    {
        var url = "https://www.w3schools.com/java/";
        var tech = new List<string> { "c++" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "vectors", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_C_Blocks_Programiz_Wrong_Section()
    {
        var url = "https://www.programiz.com/java-programming";
        var tech = new List<string> { "c" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_C_Allows_Programiz_C_Section()
    {
        var url = "https://www.programiz.com/c-programming/c-arrays";
        var tech = new List<string> { "c" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>());
        isWrong.Should().BeFalse();
    }

    [Fact]
    public void IsWrongFramework_C_Allows_W3Schools_C_Section()
    {
        var url = "https://www.w3schools.com/c/c_arrays.php";
        var tech = new List<string> { "c" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "arrays", Substitute.For<ILogger>());
        isWrong.Should().BeFalse();
    }

    [Fact]
    public void IsWrongFramework_Android_Blocks_ReactNative_And_Flutter()
    {
        var tech = new List<string> { "android" };
        SearchResultFilter.IsWrongFramework("https://reactnative.dev/docs/environment-setup", tech, SubjectCategory.Programming, "activities", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://flutter.dev/docs/get-started", tech, SubjectCategory.Programming, "activities", Substitute.For<ILogger>()).Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Flutter_Blocks_Native_Android_When_Not_Comparison()
    {
        var tech = new List<string> { "flutter" };
        SearchResultFilter.IsWrongFramework("https://example.com/android/developer-guide", tech, SubjectCategory.Programming, "widgets", Substitute.For<ILogger>()).Should().BeTrue();
        SearchResultFilter.IsWrongFramework("https://example.com/android/vs-flutter-comparison", tech, SubjectCategory.Programming, "widgets", Substitute.For<ILogger>()).Should().BeFalse();
    }

    [Fact]
    public void IsWrongFramework_SQL_Blocks_Mongoose_NoSql()
    {
        var url = "https://mongoosejs.com/docs/";
        var tech = new List<string> { "sql" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "joins", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Database_Blocks_DynamoDb_Path()
    {
        var url = "https://example.com/dynamodb/intro";
        var tech = new List<string> { "database" };
        var blocked = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "joins", Substitute.For<ILogger>());
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Blocks_JS_In_C_Context()
    {
        var url = "https://site.com/react/tutorial";
        var tech = new List<string> { "c" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "pointers", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_React_Blocks_Vue()
    {
        var url = "https://vuejs.org/guide/introduction.html";
        var tech = new List<string> { "react" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "hooks", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_React_Blocks_Angular_Domain()
    {
        var url = "https://angular.io/guide/components";
        var tech = new List<string> { "react" };
        var blocked = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "hooks", Substitute.For<ILogger>());
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_React_Blocks_Angular_Path()
    {
        var url = "https://example.com/angular/components";
        var tech = new List<string> { "react" };
        var blocked = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "hooks", Substitute.For<ILogger>());
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Vue_Blocks_React()
    {
        var url = "https://example.com/react/tutorial";
        var tech = new List<string> { "vue" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "components", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Angular_Blocks_React()
    {
        var url = "https://example.com/react/tutorial";
        var tech = new List<string> { "angular" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "components", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Flutter_Allows_Comparison_With_Native()
    {
        var url = "https://example.com/android/comparison-vs-flutter";
        var tech = new List<string> { "flutter" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "widgets", Substitute.For<ILogger>());
        isWrong.Should().BeFalse();
    }

    [Fact]
    public void IsWrongFramework_Android_Blocks_Flutter_And_ReactNative()
    {
        var tech = new List<string> { "android" };
        var b1 = SearchResultFilter.IsWrongFramework("https://flutter.dev/docs", tech, SubjectCategory.Programming, "activities", Substitute.For<ILogger>());
        var b2 = SearchResultFilter.IsWrongFramework("https://reactnative.dev/docs", tech, SubjectCategory.Programming, "activities", Substitute.For<ILogger>());
        b1.Should().BeTrue();
        b2.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Python_Blocks_OtherLangPaths()
    {
        var url = "https://example.com/java/tutorial";
        var tech = new List<string> { "python" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "tuples", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_SQL_Blocks_Mongo()
    {
        var url = "https://example.com/mongodb/introduction";
        var tech = new List<string> { "sql" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "joins", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void FilterAndPrioritizeResults_Orders_By_Relevance()
    {
        var logger = Substitute.For<ILogger>();
        var results = new[]
        {
            "Title: Hooks guide\nLink: https://react.dev/reference/react/hooks",
            "Title: Some blog\nLink: https://example.com/react/hooks",
            "Title: Angular tutorial\nLink: https://angular.io/guide/components"
        };
        var list = SearchResultFilter.FilterAndPrioritizeResults(results, "React hooks", new List<string>{"react"}, SubjectCategory.Programming, logger);
        list.Should().NotBeEmpty();
        list.First().Should().Contain("react.dev");
        list.Should().NotContain(x => x.Contains("angular.io"));
    }

    [Fact]
    public void FilterAndPrioritizeResults_MixedSources_Filters_And_Sorts()
    {
        var logger = Substitute.For<ILogger>();
        var results = new[]
        {
            "Title: Reddit\nLink: https://reddit.com/r/programming",
            "Title: W3Schools C\nLink: https://www.w3schools.com/c/c_arrays.php",
            "Title: Programiz Java\nLink: https://www.programiz.com/java-programming",
            "Title: GeeksForGeeks C\nLink: https://www.geeksforgeeks.org/c-programming-language/",
            "Title: Docs MS\nLink: https://learn.microsoft.com/en-us/dotnet/csharp/"
        };
        var list = SearchResultFilter.FilterAndPrioritizeResults(results, "C arrays", new List<string>{"c"}, SubjectCategory.Programming, logger);
        list.Should().NotBeEmpty();
        list.Should().Contain(x => x.Contains("w3schools.com/c/") || x.Contains("geeksforgeeks.org"));
        list.Should().NotContain(x => x.Contains("reddit.com"));
        list.Should().NotContain(x => x.Contains("programiz.com/java"));
        list.Should().NotContain(x => x.Contains("learn.microsoft.com"));
    }
}
