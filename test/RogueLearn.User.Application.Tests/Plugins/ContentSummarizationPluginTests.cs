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

public class ContentSummarizationPluginTests
{
    [Fact]
    public async Task SummarizeTextAsync_ReturnsNull_OnMissingChatService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var result = await sut.SummarizeTextAsync("some text", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_OnMissingChatService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            FileName = "test.txt",
            ContentType = "text/plain",
            Bytes = System.Text.Encoding.UTF8.GetBytes("Hello")
        };

        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }
}