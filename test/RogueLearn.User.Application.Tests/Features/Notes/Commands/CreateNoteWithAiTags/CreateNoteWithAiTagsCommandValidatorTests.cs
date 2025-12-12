using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;
using RogueLearn.User.Application.Options;


namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNoteWithAiTags;

public class CreateNoteWithAiTagsCommandValidatorTests
{
    private static IOptions<AiFileProcessingOptions> Opts(int maxMb = 1) => Microsoft.Extensions.Options.Options.Create(new AiFileProcessingOptions
    {
        MaxFileSizeMB = maxMb,
        AllowedMimeTypes = new[] { "application/pdf", "text/plain" }
    });

    [Fact]
    public void Missing_Content_Fails()
    {
        var v = new CreateNoteWithAiTagsCommandValidator(Opts());
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = " ", FileStream = null };
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Title_Too_Long_Fails()
    {
        var v = new CreateNoteWithAiTagsCommandValidator(Opts());
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = "x", Title = new string('a', 201) };
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MaxTags_Out_Of_Range_Fails()
    {
        var v = new CreateNoteWithAiTagsCommandValidator(Opts());
        var tooLow = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = "x", MaxTags = 0 };
        var tooHigh = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = "x", MaxTags = 21 };
        v.Validate(tooLow).IsValid.Should().BeFalse();
        v.Validate(tooHigh).IsValid.Should().BeFalse();
    }

    [Fact]
    public void File_Too_Large_Fails()
    {
        var v = new CreateNoteWithAiTagsCommandValidator(Opts(maxMb: 1));
        using var ms = new MemoryStream(new byte[2 * 1024 * 1024]);
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), FileStream = ms, FileLength = ms.Length, ContentType = "application/pdf", FileName = "a.pdf" };
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void File_Type_Not_Allowed_By_Mime_Or_Ext_Fails()
    {
        var v = new CreateNoteWithAiTagsCommandValidator(Opts());
        using var ms = new MemoryStream(new byte[100]);
        var badMime = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), FileStream = ms, FileLength = ms.Length, ContentType = "image/png", FileName = "a.png" };
        v.Validate(badMime).IsValid.Should().BeFalse();

        var goodExt = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), FileStream = ms, FileLength = ms.Length, ContentType = null, FileName = "a.txt" };
        v.Validate(goodExt).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_File_Passes()
    {
        var v = new CreateNoteWithAiTagsCommandValidator(Opts());
        using var ms = new MemoryStream(new byte[100]);
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), FileStream = ms, FileLength = ms.Length, ContentType = "application/pdf", FileName = "a.pdf" };
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Missing_Type_And_Filename_Fails()
    {
        var v = new CreateNoteWithAiTagsCommandValidator(Opts());
        using var ms = new MemoryStream(new byte[100]);
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), FileStream = ms, FileLength = ms.Length, ContentType = null, FileName = null };
        v.Validate(cmd).IsValid.Should().BeFalse();
    }
}
