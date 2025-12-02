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

public class FileTagSuggestionPluginTests
{
    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_ReturnsEmpty_OnMissingChatService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            FileName = "tags.txt",
            ContentType = "text/plain",
            Bytes = System.Text.Encoding.UTF8.GetBytes("content")
        };

        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\":");
    }
}