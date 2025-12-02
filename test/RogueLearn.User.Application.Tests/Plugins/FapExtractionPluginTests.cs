using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class FapExtractionPluginTests
{
    [Fact]
    public async Task ExtractFapRecordJsonAsync_ReturnsEmpty_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FapExtractionPlugin>>();
        var sut = new FapExtractionPlugin(kernel, logger);

        var result = await sut.ExtractFapRecordJsonAsync("transcript", CancellationToken.None);
        result.Should().BeEmpty();
    }
}