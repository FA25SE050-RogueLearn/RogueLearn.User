using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class ReadingUrlServiceTests
{
    [Fact]
    public async Task GetValidUrlForTopicAsync_UsesQueryVariants_AndDedupsUrls()
    {
        var webSearch = new Mock<IWebSearchService>();
        var urlValidation = new Mock<IUrlValidationService>();
        var classification = new Mock<IAiQueryClassificationService>();
        var logger = new Mock<ILogger<ReadingUrlService>>();

        var service = new ReadingUrlService(webSearch.Object, urlValidation.Object, classification.Object, logger.Object);

        var topic = "Hàm và Tham số"; // Vietnamese
        var subjectContext = "Functions and Parameters"; // English

        // Two variant searches return overlapping results with tracking params
        webSearch.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string q, int _, int __, CancellationToken ___) =>
            {
                var r1 = $"Title: T\nLink: https://viblo.asia/p/ham-va-tham-so?utm_source=abc\nSnippet: S";
                var r2 = $"Title: T\nLink: https://viblo.asia/p/ham-va-tham-so\nSnippet: S";
                var r3 = $"Title: T\nLink: https://topdev.vn/blog/cau-hoi-ve-ham\nSnippet: S";
                if (q.Contains("hướng dẫn") || q.Contains("bài giảng"))
                {
                    return new[] { r1, r2 };
                }
                return new[] { r2, r3 };
            });

        // Validation passes only for canonical first URL
        urlValidation.Setup(v => v.IsUrlAccessibleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string url, CancellationToken _) => url.StartsWith("https://viblo.asia/p/ham-va-tham-so"));

        var existingReadings = new[] { "https://topdev.vn/blog/cau-hoi-ve-ham" };
        var result = await service.GetValidUrlForTopicAsync(topic, existingReadings, subjectContext, SubjectCategory.Programming, null, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().Be("https://viblo.asia/p/ham-va-tham-so");
    }

    [Fact]
    public void BuildQueryVariants_GeneratesEnglishAndVietnamese()
    {
        var topic = "Mảng và Con trỏ";
        var ctx = "Pointers and Arrays";

        var variants = SearchQueryBuilder.BuildQueryVariants(topic, ctx, SubjectCategory.Programming);
        variants.Should().Contain(v => v.Contains("tutorial"));
        variants.Should().Contain(v => v.Contains("hướng dẫn"));
    }
}
