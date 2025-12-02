using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class CurriculumExtractionPluginTests
{
    [Fact]
    public async Task ExtractCurriculumJsonAsync_ReturnsEmpty_WhenPromptMissing()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<CurriculumExtractionPlugin>>();
        var sut = new CurriculumExtractionPlugin(kernel, logger);

        var result = await sut.ExtractCurriculumJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }
}