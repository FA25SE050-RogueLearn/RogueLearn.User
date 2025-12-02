using System.Collections.Generic;
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
}