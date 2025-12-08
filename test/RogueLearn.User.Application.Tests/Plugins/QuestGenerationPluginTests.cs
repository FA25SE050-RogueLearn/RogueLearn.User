using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Plugins;

public class QuestGenerationPluginTests
{
    

    [Fact]
    public void BuildRetryErrorHint_ContainsPreviousErrorText()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<QuestGenerationPlugin>>();
        var sut = new QuestGenerationPlugin(kernel, logger);

        var m = typeof(QuestGenerationPlugin).GetMethod("BuildRetryErrorHint", BindingFlags.NonPublic | BindingFlags.Instance);
        var hint = (string)m!.Invoke(sut, new object[] { "some error" })!;
        hint.Should().Contain("PREVIOUS ATTEMPT FAILED");
        hint.Should().Contain("some error");
    }

    [Fact]
    public void BuildRetryErrorHint_ReturnsEmpty_WhenNoError()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<QuestGenerationPlugin>>();
        var sut = new QuestGenerationPlugin(kernel, logger);
        var m = typeof(QuestGenerationPlugin).GetMethod("BuildRetryErrorHint", BindingFlags.NonPublic | BindingFlags.Instance);
        var hint = (string)m!.Invoke(sut, new object[] { null! })!;
        hint.Should().BeEmpty();
    }

    

    

    

    

    

    

    

    

    [Fact]
    public async Task GenerateFromPromptAsync_ReturnsNull_WhenKernelFails()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<QuestGenerationPlugin>>();
        var sut = new QuestGenerationPlugin(kernel, logger);

        var week = new RogueLearn.User.Application.Models.WeekContext
        {
            WeekNumber = 1,
            TotalWeeks = 10,
            TopicsToCover = new List<string> { "A" },
            AvailableResources = new List<RogueLearn.User.Application.Models.ValidResource>()
        };
        var academic = new RogueLearn.User.Application.Models.AcademicContext
        {
            CurrentGpa = 7.5,
            AttemptReason = RogueLearn.User.Application.Models.QuestAttemptReason.FirstTime,
            PrerequisiteHistory = new List<RogueLearn.User.Application.Models.PrerequisitePerformance>(),
            RelatedSubjects = new List<RogueLearn.User.Application.Models.RelatedSubjectGrade>(),
            PreviousAttempts = 0,
            StrengthAreas = new List<string> { "Math" },
            ImprovementAreas = new List<string> { "Reading" }
        };
        var skills = new List<RogueLearn.User.Domain.Entities.Skill>();

        var prompt = "Test prompt";
        var result = await sut.GenerateFromPromptAsync(prompt, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateFromPromptAsync_RetryWithoutErrorHint_ReturnsNull_WhenKernelUnavailable()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<QuestGenerationPlugin>>();
        var sut = new QuestGenerationPlugin(kernel, logger);

        var week = new RogueLearn.User.Application.Models.WeekContext
        {
            WeekNumber = 2,
            TotalWeeks = 10,
            TopicsToCover = new List<string> { "B" },
            AvailableResources = new List<RogueLearn.User.Application.Models.ValidResource>()
        };
        var academic = new RogueLearn.User.Application.Models.AcademicContext
        {
            CurrentGpa = 8.6,
            AttemptReason = RogueLearn.User.Application.Models.QuestAttemptReason.FirstTime,
            PrerequisiteHistory = new List<RogueLearn.User.Application.Models.PrerequisitePerformance>(),
            RelatedSubjects = new List<RogueLearn.User.Application.Models.RelatedSubjectGrade>(),
            PreviousAttempts = 0,
            StrengthAreas = new List<string> { "Science" },
            ImprovementAreas = new List<string> { "Writing" }
        };
        var skills = new List<RogueLearn.User.Domain.Entities.Skill>();

        var prompt = "Another prompt";
        var result = await sut.GenerateFromPromptAsync(prompt, CancellationToken.None);
        result.Should().BeNull();
    }

    
}
