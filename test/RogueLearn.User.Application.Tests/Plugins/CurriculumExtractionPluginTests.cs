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

public class CurriculumExtractionPluginTests
{
    [Fact]
    public async Task ExtractCurriculumJsonAsync_ReturnsEmpty_WhenPromptMissing()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<CurriculumExtractionPlugin>>();
        var sut = new CurriculumExtractionPlugin(kernel, logger);

        var result = await sut.ExtractCurriculumJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public void CleanToJson_StripsFencesAndExtractsObject()
    {
        var raw = "```json\n{ \"program\": { \"programName\": \"X\" } }\n```";
        var m = typeof(CurriculumExtractionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.EndWith("}");
        cleaned.Should().Contain("programName");
    }

    [Fact]
    public void CleanToJson_StripsGenericFenceAndExtractsObject()
    {
        var raw = "```\n{ \"program\": { \"programName\": \"Y\" } }\n```";
        var m = typeof(CurriculumExtractionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.EndWith("}");
        cleaned.Should().Contain("programName");
    }

    [Fact]
    public void CleanToJson_ExtractsBracesFromWrappedText()
    {
        var raw = "prefix { \"program\": { \"programName\": \"Z\" } } suffix";
        var m = typeof(CurriculumExtractionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Be("{ \"program\": { \"programName\": \"Z\" } }");
    }

    [Fact]
    public async Task ExtractCurriculumJsonAsync_ReturnsEmpty_OnKernelFailure_WithPromptPresent()
    {
        var baseDir = AppContext.BaseDirectory;
        var promptDir = Path.Combine(baseDir, "Features", "CurriculumImport", "Prompts");
        Directory.CreateDirectory(promptDir);
        var promptPath = Path.Combine(promptDir, "ExtractCurriculumPrompt.txt");
        await File.WriteAllTextAsync(promptPath, "Template {{RAW_TEXT}}", CancellationToken.None);

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<CurriculumExtractionPlugin>>();
        var sut = new CurriculumExtractionPlugin(kernel, logger);

        var result = await sut.ExtractCurriculumJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractCurriculumJsonAsync_ReturnsEmpty_OnMissingPromptFile()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<CurriculumExtractionPlugin>>();
        var sut = new CurriculumExtractionPlugin(kernel, logger);
        var res = await sut.ExtractCurriculumJsonAsync("raw", CancellationToken.None);
        res.Should().BeEmpty();
    }

    
}
