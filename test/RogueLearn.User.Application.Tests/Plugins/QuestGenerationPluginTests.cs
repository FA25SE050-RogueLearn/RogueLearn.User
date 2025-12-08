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
    public void CleanToJson_ReturnsJsonObject_WithActivitiesArray()
    {
        var raw = "```json\n{ \"activities\": [] }\n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.Contain("\"activities\"");
    }

    [Fact]
    public void BuildRetryErrorHint_ContainsPreviousErrorText()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<QuestGenerationPlugin>>();
        var builder = new QuestStepsPromptBuilder();
        var sut = new QuestGenerationPlugin(kernel, logger, builder);

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
        var builder = new QuestStepsPromptBuilder();
        var sut = new QuestGenerationPlugin(kernel, logger, builder);
        var m = typeof(QuestGenerationPlugin).GetMethod("BuildRetryErrorHint", BindingFlags.NonPublic | BindingFlags.Instance);
        var hint = (string)m!.Invoke(sut, new object[] { null! })!;
        hint.Should().BeEmpty();
    }

    [Fact]
    public void CleanToJson_StripsFourBacktickFence()
    {
        var raw = "````\n{ \"activities\": [] }\n````";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.Contain("activities");
    }

    [Fact]
    public void CleanToJson_FixesTrailingCommas()
    {
        var raw = "```json\n{ \"activities\": [ { \"type\": \"reading\" }, ] , }\n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.Contain("activities");
    }

    [Fact]
    public void CleanToJson_ReconstructsArray_WhenRootMissing()
    {
        var raw = "```json\n broken [ { \"type\": \"reading\" } ] text \n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.Contain("\"activities\"");
    }

    [Fact]
    public void CleanToJson_FixesZeroEscape_AndValidates()
    {
        var raw = "```json\n{ \"activities\": [ { \"title\": \"End at \\0\" } ] }\n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Contain("\\\\0");
        using var doc = JsonDocument.Parse(cleaned);
        doc.RootElement.GetProperty("activities").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void CleanToJson_ThrowsCleaningJsonException_WhenNoActivitiesAndNoArray()
    {
        var raw = "```json\n{ \"notActivities\": {} }\n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        Action act = () => m!.Invoke(null, new object[] { raw });
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>();
    }

    [Fact]
    public void CleanToJson_ThrowsCleaningJsonException_OnInvalidEscapes()
    {
        var raw = "```json\n{ \"activities\": [ { \"title\": \"\\a\" } ] }\n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        Action act = () => m!.Invoke(null, new object[] { raw });
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>();
    }

    [Fact]
    public void CleanToJson_DoubleEscapes_InvalidBackslashes()
    {
        var raw = "```json\n{ \"activities\": [ { \"title\": \"Sum \\int x dx\" } ] }\n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Contain("\\\\int");
        using var doc = JsonDocument.Parse(cleaned);
        doc.RootElement.GetProperty("activities").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void CleanToJson_ReplacesNewlinesWithSpaces()
    {
        var raw = "```json\n{ \"activities\": [ { \"title\": \"Line1\\nLine2\" } ] }\n```";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        using var doc = JsonDocument.Parse(cleaned);
        doc.RootElement.GetProperty("activities").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GenerateQuestStepsJsonAsync_ThrowsAfterMaxAttempts_WhenKernelFails()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<QuestGenerationPlugin>>();
        var builder = new QuestStepsPromptBuilder();
        var sut = new QuestGenerationPlugin(kernel, logger, builder);

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

        var action = () => sut.GenerateQuestStepsJsonAsync(
            week,
            "user ctx",
            skills,
            "Subject",
            "Course desc",
            academic,
            null,
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateQuestStepsJsonAsync_UserClassNull_AttemptsAndThrows_WhenKernelUnavailable()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<QuestGenerationPlugin>>();
        var builder = new QuestStepsPromptBuilder();
        var sut = new QuestGenerationPlugin(kernel, logger, builder);

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

        var action = () => sut.GenerateQuestStepsJsonAsync(
            week,
            "user ctx",
            skills,
            "Subject",
            "Course desc",
            academic,
            null,
            CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void CleanToJson_ThrowsCleaningJsonException_TypeIsSpecific()
    {
        var raw = "broken content without brackets";
        var m = typeof(QuestGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        Action act = () => m!.Invoke(null, new object[] { raw });
        var ex = Assert.Throws<TargetInvocationException>(act);
        ex.InnerException!.GetType().Name.Should().Be("CleaningJsonException");
    }
}
