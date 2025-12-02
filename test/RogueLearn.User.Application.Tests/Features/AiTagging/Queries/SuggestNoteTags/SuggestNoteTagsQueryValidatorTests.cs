using FluentAssertions;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.AiTagging.Queries.SuggestNoteTags;

public class SuggestNoteTagsQueryValidatorTests
{
    [Fact]
    public void Valid_Passes()
    {
        var q = new SuggestNoteTagsQuery { AuthUserId = System.Guid.NewGuid(), RawText = "text", MaxTags = 5 };
        var validator = new SuggestNoteTagsQueryValidator();
        var res = validator.Validate(q);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_NoSource_Fails()
    {
        var q = new SuggestNoteTagsQuery { AuthUserId = System.Guid.NewGuid(), RawText = null, NoteId = null };
        var validator = new SuggestNoteTagsQueryValidator();
        var res = validator.Validate(q);
        res.IsValid.Should().BeFalse();
    }
}