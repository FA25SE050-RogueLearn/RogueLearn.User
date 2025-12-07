using System.Collections.Generic;
using System.Reflection;
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

    [Fact]
    public void CleanToJson_StripsFences_AndExtractsObject()
    {
        var raw = "```\n{ \"subjectCode\": \"ABC\", \"credits\": 3 }\n```";
        var m = typeof(SubjectExtractionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{");
        cleaned.Should().Contain("subjectCode");
    }

    [Fact]
    public async Task ExtractSkillsFromObjectivesAsync_ReturnsEmptyStrings_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SubjectExtractionPlugin>>();
        var sut = new SubjectExtractionPlugin(kernel, logger);
        var objectives = new List<string> { "Learn A", "Practice B", "Understand C" };
        var res = await sut.ExtractSkillsFromObjectivesAsync(objectives, CancellationToken.None);
        res.Should().HaveCount(objectives.Count);
    }

    [Fact]
    public async Task ExtractSkillsFromObjectivesAsync_ReturnsEmpty_WhenInputEmpty()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SubjectExtractionPlugin>>();
        var sut = new SubjectExtractionPlugin(kernel, logger);
        var res = await sut.ExtractSkillsFromObjectivesAsync(new List<string>(), CancellationToken.None);
        res.Should().BeEmpty();
    }
}