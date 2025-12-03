using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class SubjectExtractionPluginTests
{
    [Fact]
    public async Task ExtractSubjectJsonAsync_ReturnsEmpty_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SubjectExtractionPlugin>>();
        var sut = new SubjectExtractionPlugin(kernel, logger);

        var result = await sut.ExtractSubjectJsonAsync("subject text", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractSkillsFromObjectivesAsync_ReturnsEmptyList_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SubjectExtractionPlugin>>();
        var sut = new SubjectExtractionPlugin(kernel, logger);

        var lo = new List<string> { "Learn pointers", "Understand arrays" };
        var result = await sut.ExtractSkillsFromObjectivesAsync(lo, CancellationToken.None);
        result.Should().HaveCount(lo.Count);
        result.Should().AllSatisfy(s => s.Should().Be(string.Empty));
    }
}