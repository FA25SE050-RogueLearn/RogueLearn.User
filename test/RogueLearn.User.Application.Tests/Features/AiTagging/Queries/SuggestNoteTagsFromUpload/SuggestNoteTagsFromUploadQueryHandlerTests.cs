using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoFile_Throws()
    {
        var service = Substitute.For<ITaggingSuggestionService>();
        var sut = new SuggestNoteTagsFromUploadQueryHandler(service);
        var query = new SuggestNoteTagsFromUploadQuery { AuthUserId = Guid.NewGuid(), FileStream = null, FileLength = 0 };
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithFile_ReturnsSuggestions()
    {
        var service = Substitute.For<ITaggingSuggestionService>();
        var sut = new SuggestNoteTagsFromUploadQueryHandler(service);
        var query = new SuggestNoteTagsFromUploadQuery { AuthUserId = Guid.NewGuid(), FileStream = new MemoryStream(new byte[] { 1, 2 }), FileLength = 2, ContentType = "application/pdf", FileName = "file.pdf" };
        service.SuggestFromFileAsync(query.AuthUserId, Arg.Any<AiFileAttachment>(), query.MaxTags, Arg.Any<CancellationToken>()).Returns(new System.Collections.Generic.List<TagSuggestionDto> { new() { Label = "tag", Confidence = 0.8 } });
        var res = await sut.Handle(query, CancellationToken.None);
        res.Suggestions.Should().HaveCount(1);
    }
}