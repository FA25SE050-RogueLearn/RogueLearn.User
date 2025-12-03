using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class SyllabusExtractionPluginTests
{
    [Fact]
    public async Task ExtractSyllabusJsonAsync_ReturnsEmpty_WhenPromptMissing()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SyllabusExtractionPlugin>>();
        var sut = new SyllabusExtractionPlugin(kernel, logger);

        var result = await sut.ExtractSyllabusJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }
}