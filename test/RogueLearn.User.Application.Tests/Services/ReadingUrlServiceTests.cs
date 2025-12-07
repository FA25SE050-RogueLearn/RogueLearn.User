using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class ReadingUrlServiceTests
{
    private static ReadingUrlService CreateSut(
        IWebSearchService? web = null,
        IUrlValidationService? urlVal = null,
        IAiQueryClassificationService? ai = null,
        ILogger<ReadingUrlService>? logger = null)
    {
        web ??= Substitute.For<IWebSearchService>();
        urlVal ??= Substitute.For<IUrlValidationService>();
        ai ??= Substitute.For<IAiQueryClassificationService>();
        logger ??= Substitute.For<ILogger<ReadingUrlService>>();
        return new ReadingUrlService(web, urlVal, ai, logger);
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_Chooses_Syllabus_Reading_When_Accessible_And_Relevant()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var readings = new[] { "https://react.dev/learn" };
        urlVal.IsUrlAccessibleAsync(readings[0], Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "React hooks",
            readings: readings,
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().Be("https://react.dev/learn");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_Uses_OverrideQueries_And_Returns_Validated_Url()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var overrideQueries = new List<string> { "Android RecyclerView tutorial" };
        web.SearchAsync(overrideQueries[0], Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]
            {
                "Title: RecyclerView\nLink: https://developer.android.com/guide/topics/ui/layout/recyclerview"
            }));
        urlVal.IsUrlAccessibleAsync("https://developer.android.com/guide/topics/ui/layout/recyclerview", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "RecyclerView",
            readings: Array.Empty<string>(),
            subjectContext: "Android",
            category: SubjectCategory.Programming,
            overrideQueries: overrideQueries,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().Be("https://developer.android.com/guide/topics/ui/layout/recyclerview");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_Falls_Back_To_OfficialDocs_If_No_Results()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        // Provide one result that is not accessible, forcing fallback after try block
        web.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]
            {
                "Title: RecyclerView\nLink: https://developer.android.com/some/other"
            }));

        // Block the search result URL specifically
        urlVal.IsUrlAccessibleAsync("https://developer.android.com/some/other", Arg.Any<CancellationToken>())
            .Returns(false);

        // Allow only the specific official doc on fallback
        urlVal.IsUrlAccessibleAsync(Arg.Is<string>(u => u.Contains("developer.android.com") && u.Contains("recyclerview")), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "RecyclerView",
            readings: Array.Empty<string>(),
            subjectContext: "Android",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().Contain("developer.android.com");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_OfficialDocs_Accessible_Returns_It()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        web.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]{ "Title: Android Guide\nLink: https://example.com/android/guide" }));

        urlVal.IsUrlAccessibleAsync(Arg.Is<string>(u => u.Contains("developer.android.com")), Arg.Any<CancellationToken>())
            .Returns(true);
        urlVal.IsUrlAccessibleAsync(Arg.Is<string>(u => u.Contains("example.com/android/guide")), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "RecyclerView",
            readings: Array.Empty<string>(),
            subjectContext: "Android",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().Contain("developer.android.com");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_OfficialDocs_Used_Skips_And_Returns_Null()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        web.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(Array.Empty<string>()));

        urlVal.IsUrlAccessibleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "RecyclerView",
            readings: Array.Empty<string>(),
            subjectContext: "Android",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: u => u.Contains("developer.android.com"),
            cancellationToken: CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_LLM_Generates_Queries_Then_Picks_First_Accessible()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.GenerateQueryVariantsAsync("Hooks", "React", SubjectCategory.Programming, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<string>{ "React hooks tutorial", "React hooks guide" }));
        var sut = CreateSut(web, urlVal, ai);

        web.SearchAsync("React hooks tutorial", Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]{ "Title: React learn\nLink: https://react.dev/learn" }));
        urlVal.IsUrlAccessibleAsync("https://react.dev/learn", Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "Hooks",
            readings: Array.Empty<string>(),
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().Be("https://react.dev/learn");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_LLM_Failure_Falls_Back_To_Rule_Based_Queries()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        ai.GenerateQueryVariantsAsync(Arg.Any<string>(), Arg.Any<string>(), SubjectCategory.Programming, Arg.Any<CancellationToken>())
            .Returns<Task<List<string>>>(_ => throw new InvalidOperationException("llm fail"));

        web.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]{ "Title: React learn\nLink: https://react.dev/learn" }));
        urlVal.IsUrlAccessibleAsync("https://react.dev/learn", Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "Hooks",
            readings: Array.Empty<string>(),
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().Be("https://react.dev/learn");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_Handles_429_Then_Succeeds_On_Next_Query()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var queries = new List<string> { "q1", "q2" };
        // First query throws 429
        web.SearchAsync(queries[0], Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<IEnumerable<string>>>(_ => throw new HttpRequestException("Too Many Requests", null, (System.Net.HttpStatusCode)429));
        // Second query returns a valid link
        web.SearchAsync(queries[1], Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]
            {
                "Title: Hooks\nLink: https://react.dev/learn"
            }));
        urlVal.IsUrlAccessibleAsync("https://react.dev/learn", Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "React hooks",
            readings: Array.Empty<string>(),
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: queries,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().Contain("react.dev");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_QueryVariantThrows_GeneralException_Then_Next_Works()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var queries = new List<string> { "v1", "v2" };
        web.SearchAsync(queries[0], Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<IEnumerable<string>>>(_ => throw new InvalidOperationException("boom"));
        web.SearchAsync(queries[1], Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]{ "Title: Learn\nLink: https://react.dev/learn" }));
        urlVal.IsUrlAccessibleAsync("https://react.dev/learn", Arg.Any<CancellationToken>()).Returns(true);

        var res = await sut.GetValidUrlForTopicAsync(
            topic: "React hooks",
            readings: Array.Empty<string>(),
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: queries,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        res.Should().Be("https://react.dev/learn");
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_AllResultsFiltered_Returns_Null()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var queries = new List<string> { "q1" };
        web.SearchAsync(queries[0], Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]
            {
                "Title: Reddit thread\nLink: https://reddit.com/r/programming",
                "Title: Wrong framework\nLink: https://angular.io/guide/components"
            }));

        var res = await sut.GetValidUrlForTopicAsync(
            topic: "React hooks",
            readings: Array.Empty<string>(),
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: queries,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        res.Should().BeNull();
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_Skips_Used_Url()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var readings = new[] { "https://react.dev/learn" };
        urlVal.IsUrlAccessibleAsync(readings[0], Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "React hooks",
            readings: readings,
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: u => u.Contains("react.dev"),
            cancellationToken: CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_Tier1_Accessible_But_Irrelevant_Skips()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var readings = new[] { "https://www.w3schools.com/python/python_tuples.asp" };
        urlVal.IsUrlAccessibleAsync(readings[0], Arg.Any<CancellationToken>()).Returns(true);

        web.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(Array.Empty<string>()));

        urlVal.IsUrlAccessibleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "C arrays",
            readings: readings,
            subjectContext: "C",
            category: SubjectCategory.Programming,
            overrideQueries: null,
            isUrlUsedCheck: _ => false,
            cancellationToken: CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValidUrlForTopicAsync_Tier2_Deduplicates_Existing_And_Duplicate_Results()
    {
        var web = Substitute.For<IWebSearchService>();
        var urlVal = Substitute.For<IUrlValidationService>();
        var ai = Substitute.For<IAiQueryClassificationService>();
        var sut = CreateSut(web, urlVal, ai);

        var readings = new[] { "https://react.dev/learn" };
        var queries = new List<string> { "q1" };
        web.SearchAsync(queries[0], Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(new[]
            {
                "Title: React Learn\nLink: https://react.dev/learn",
                "Title: Hooks Guide\nLink: https://react.dev/reference/react/hooks"
            }));
        urlVal.IsUrlAccessibleAsync("https://react.dev/learn", Arg.Any<CancellationToken>()).Returns(true);
        urlVal.IsUrlAccessibleAsync("https://react.dev/reference/react/hooks", Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.GetValidUrlForTopicAsync(
            topic: "React hooks",
            readings: readings,
            subjectContext: "React",
            category: SubjectCategory.Programming,
            overrideQueries: queries,
            isUrlUsedCheck: u => u.Contains("react.dev/learn"),
            cancellationToken: CancellationToken.None);

        result.Should().Be("https://react.dev/reference/react/hooks");
    }
}
