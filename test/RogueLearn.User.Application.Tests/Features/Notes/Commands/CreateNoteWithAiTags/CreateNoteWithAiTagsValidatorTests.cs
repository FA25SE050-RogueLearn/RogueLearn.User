using FluentAssertions;
using Microsoft.Extensions.Options;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;
using RogueLearn.User.Application.Options;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNoteWithAiTags;

public class CreateNoteWithAiTagsValidatorTests
{
    private static IOptions<AiFileProcessingOptions> Opts() => Microsoft.Extensions.Options.Options.Create(new AiFileProcessingOptions { MaxFileSizeMB = 1, AllowedMimeTypes = new[] { "text/plain" } });

    [Fact]
    public void Invalid_WhenNoContent()
    {
        var validator = new CreateNoteWithAiTagsCommandValidator(Opts());
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid() };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_WithRawText()
    {
        var validator = new CreateNoteWithAiTagsCommandValidator(Opts());
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = "text" };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_FileTooLargeOrType()
    {
        var validator = new CreateNoteWithAiTagsCommandValidator(Opts());
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), FileStream = new MemoryStream(new byte[10]), FileLength = 3_000_000, ContentType = "application/pdf", FileName = "a.pdf" };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }
}