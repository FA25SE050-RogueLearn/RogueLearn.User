using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class RoadmapExtractionPluginTests
{
    [Fact]
    public async Task ExtractClassRoadmapJsonAsync_ReturnsEmpty_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<RoadmapExtractionPlugin>>();
        var sut = new RoadmapExtractionPlugin(kernel, logger);

        var result = await sut.ExtractClassRoadmapJsonAsync("roadmap text", CancellationToken.None);
        result.Should().BeEmpty();
    }
}