using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class SyllabusExtractionPluginTests
{
    [Fact]
    public async Task ExtractSyllabusJsonAsync_ReturnsEmpty_WhenPromptMissing()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SyllabusExtractionPlugin>>();
        var sut = new SyllabusExtractionPlugin(kernel, logger);

        var result = await sut.ExtractSyllabusJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public void CleanToJson_StripsFencesAndExtractsObject()
    {
        var raw = "```\n{ \"subjectCode\": \"ABC\", \"syllabusName\": \"Name\" }\n```";
        var m = typeof(SyllabusExtractionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.EndWith("}");
        cleaned.Should().Contain("subjectCode");
    }

    [Fact]
    public void CleanToJson_StripsJsonFence()
    {
        var raw = "```json\n{ \"subjectCode\": \"XYZ\" }\n```";
        var m = typeof(SyllabusExtractionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.EndWith("}");
        cleaned.Should().Contain("subjectCode");
    }

    [Fact]
    public void CleanToJson_ExtractsBracesFromWrappedText()
    {
        var raw = "noise { \"subjectCode\": \"DEF\" } trailing";
        var m = typeof(SyllabusExtractionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Be("{ \"subjectCode\": \"DEF\" }");
    }

    [Fact]
    public async Task ExtractSyllabusJsonAsync_ReturnsEmpty_OnKernelFailure_WithPromptPresent()
    {
        var baseDir = AppContext.BaseDirectory;
        var promptDir = Path.Combine(baseDir, "Features", "CurriculumImport", "Prompts");
        Directory.CreateDirectory(promptDir);
        var promptPath = Path.Combine(promptDir, "ExtractSyllabusPrompt.txt");
        await File.WriteAllTextAsync(promptPath, "Template {{RAW_TEXT}}", CancellationToken.None);

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SyllabusExtractionPlugin>>();
        var sut = new SyllabusExtractionPlugin(kernel, logger);

        var result = await sut.ExtractSyllabusJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractSyllabusJsonAsync_ReturnsEmpty_OnMissingPromptFile()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<SyllabusExtractionPlugin>>();
        var sut = new SyllabusExtractionPlugin(kernel, logger);

        var result = await sut.ExtractSyllabusJsonAsync("text", CancellationToken.None);
        result.Should().BeEmpty();
    }
}
