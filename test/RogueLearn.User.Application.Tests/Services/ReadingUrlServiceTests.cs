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
}