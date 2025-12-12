using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;
using RogueLearn.User.Application.Options;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQueryValidatorTests
{
    private static IOptions<AiFileProcessingOptions> CreateOptions() => Microsoft.Extensions.Options.Options.Create(new AiFileProcessingOptions { MaxFileSizeMB = 4, AllowedMimeTypes = new[] { "application/pdf", "text/plain" } });

    [Fact]
    public void Valid_Passes()
    {
        var validator = new SuggestNoteTagsFromUploadQueryValidator(CreateOptions());
        var q = new SuggestNoteTagsFromUploadQuery { AuthUserId = System.Guid.NewGuid(), FileStream = new MemoryStream(new byte[10]), FileLength = 10, ContentType = "application/pdf", FileName = "file.pdf", MaxTags = 5 };
        var res = validator.Validate(q);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Type_Fails()
    {
        var validator = new SuggestNoteTagsFromUploadQueryValidator(CreateOptions());
        var q = new SuggestNoteTagsFromUploadQuery { AuthUserId = System.Guid.NewGuid(), FileStream = new MemoryStream(new byte[10]), FileLength = 10, ContentType = "image/png", FileName = "file.png", MaxTags = 5 };
        var res = validator.Validate(q);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Missing_Type_And_Filename_Fails()
    {
        var validator = new SuggestNoteTagsFromUploadQueryValidator(CreateOptions());
        var q = new SuggestNoteTagsFromUploadQuery { AuthUserId = System.Guid.NewGuid(), FileStream = new MemoryStream(new byte[10]), FileLength = 10, ContentType = string.Empty, FileName = string.Empty, MaxTags = 5 };
        var res = validator.Validate(q);
        res.IsValid.Should().BeFalse();
    }
}
