using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class ConstructiveQuestionGenerationPluginTests
{
    [Fact]
    public async Task GenerateQuestionsAsync_ReturnsEmpty_WhenPromptMissing()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ConstructiveQuestionGenerationPlugin>>();
        var sut = new ConstructiveQuestionGenerationPlugin(kernel, logger);

        var result = await sut.GenerateQuestionsAsync(new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "Intro" }
        }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public void CleanToJson_StripsJsonFenceAndExtractsArray()
    {
        var raw = "```json\n[ { \"q\": \"A?\" } ]\n```";
        var m = typeof(ConstructiveQuestionGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("[").And.EndWith("]");
        cleaned.Should().Contain("\"q\"");
    }

    [Fact]
    public void CleanToJson_StripsGenericFenceAndExtractsArray()
    {
        var raw = "```\n[ { \"x\": 1 } ]\n```";
        var m = typeof(ConstructiveQuestionGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Be("[ { \"x\": 1 } ]");
    }

    [Fact]
    public void CleanToJson_ExtractsArrayFromWrappedText()
    {
        var raw = "noise [ { \"q\": \"A?\" } ] trailing";
        var m = typeof(ConstructiveQuestionGenerationPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Be("[ { \"q\": \"A?\" } ]");
    }

    [Fact]
    public async Task GenerateQuestionsAsync_ReturnsEmpty_OnMissingPromptFile()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ConstructiveQuestionGenerationPlugin>>();
        var sut = new ConstructiveQuestionGenerationPlugin(kernel, logger);

        var schedule = new List<SyllabusSessionDto>
        {
            new SyllabusSessionDto { SessionNumber = 1, Topic = "Intro" }
        };

        var res = await sut.GenerateQuestionsAsync(schedule, CancellationToken.None);
        res.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateQuestionsAsync_ReturnsEmpty_OnKernelFailure_WithPromptPresent()
    {
        var baseDir = AppContext.BaseDirectory;
        var promptDir = Path.Combine(baseDir, "Features", "CurriculumImport", "Prompts");
        Directory.CreateDirectory(promptDir);
        var promptPath = Path.Combine(promptDir, "GenerateConstructiveQuestionsPrompt.txt");
        await File.WriteAllTextAsync(promptPath, "Template {{SESSION_SCHEDULE_JSON}}", CancellationToken.None);

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ConstructiveQuestionGenerationPlugin>>();
        var sut = new ConstructiveQuestionGenerationPlugin(kernel, logger);

        var schedule = new List<SyllabusSessionDto>
        {
            new SyllabusSessionDto { SessionNumber = 1, Topic = "Intro" }
        };

        var res = await sut.GenerateQuestionsAsync(schedule, CancellationToken.None);
        res.Should().BeEmpty();
    }
}
