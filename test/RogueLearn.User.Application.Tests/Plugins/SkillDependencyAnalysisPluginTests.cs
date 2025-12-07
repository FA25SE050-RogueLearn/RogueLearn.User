using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;

namespace RogueLearn.User.Application.Tests.Plugins;

public class SkillDependencyAnalysisPluginTests
{
    [Fact]
    public async Task AnalyzeSkillDependenciesAsync_ReturnsEmpty_WhenNoAiService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SkillDependencyAnalysisPlugin>>();
        var sut = new SkillDependencyAnalysisPlugin(kernel, logger);

        var skills = new List<string> { "RecyclerView", "Adapters" };
        var res = await sut.AnalyzeSkillDependenciesAsync(skills, CancellationToken.None);
        res.Should().NotBeNull();
        res.Should().BeEmpty();
    }

    [Fact]
    public void CleanToJson_RemovesCodeFences_And_ExtractsArray()
    {
        var raw = "```json\n[ { \"skillName\": \"A\", \"prerequisiteSkillName\": \"B\", \"reasoning\": \"r\" } ]\n```";
        var m = typeof(SkillDependencyAnalysisPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("[");
        cleaned.Should().EndWith("]");
        cleaned.Should().Contain("skillName");
    }

    [Fact]
    public void CleanToJson_ExtractsBracketedSubarray_WhenWrapped()
    {
        var raw = "prefix text [ { \"skillName\": \"X\", \"prerequisiteSkillName\": \"Y\", \"reasoning\": \"z\" } ] suffix";
        var m = typeof(SkillDependencyAnalysisPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Be("[ { \"skillName\": \"X\", \"prerequisiteSkillName\": \"Y\", \"reasoning\": \"z\" } ]");
    }

    [Fact]
    public void Defaults_Are_Set_Correctly()
    {
        var m = new SkillDependencyAnalysis();
        m.SkillName.Should().BeEmpty();
        m.PrerequisiteSkillName.Should().BeEmpty();
        m.RelationshipType.Should().Be("Prerequisite");
        m.Reasoning.Should().BeEmpty();
    }
}