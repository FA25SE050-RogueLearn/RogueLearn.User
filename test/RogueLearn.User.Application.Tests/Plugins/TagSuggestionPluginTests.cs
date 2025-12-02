using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class TagSuggestionPluginTests
{
    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_ReturnsEmpty_OnKernelFailure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<TagSuggestionPlugin>>();
        var sut = new TagSuggestionPlugin(kernel, logger);

        var json = await sut.GenerateTagSuggestionsJsonAsync("Some text", 10, CancellationToken.None);
        json.Should().Contain("\"tags\":");
    }
}