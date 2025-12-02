using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class FlmExtractionPluginTests
{
    [Fact]
    public async Task ExtractCurriculumJsonAsync_ReturnsEmpty_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FlmExtractionPlugin>>();
        var sut = new FlmExtractionPlugin(kernel, logger);

        var result = await sut.ExtractCurriculumJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractSyllabusJsonAsync_ReturnsEmpty_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FlmExtractionPlugin>>();
        var sut = new FlmExtractionPlugin(kernel, logger);

        var result = await sut.ExtractSyllabusJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }
}