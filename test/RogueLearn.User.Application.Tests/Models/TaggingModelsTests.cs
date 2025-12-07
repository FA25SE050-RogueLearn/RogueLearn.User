using System;
using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class TaggingModelsTests
{
    [Fact]
    public void TagSuggestionDto_IsExisting_DependsOnMatchedTagId()
    {
        var dto = new TagSuggestionDto { Label = "jwt", Confidence = 0.9, Reason = "security" };
        dto.IsExisting.Should().BeFalse();
        dto.MatchedTagId = Guid.NewGuid();
        dto.MatchedTagName = "JWT";
        dto.IsExisting.Should().BeTrue();
    }

    [Fact]
    public void SuggestAndCommitResponses_Can_Set_Properties()
    {
        var created = new CreatedTagDto { Id = Guid.NewGuid(), Name = "dotnet" };
        var commit = new CommitNoteTagSelectionsResponse
        {
            NoteId = Guid.NewGuid(),
            AddedTagIds = new[] { Guid.NewGuid(), created.Id },
            CreatedTags = new[] { created },
            TotalTagsAssigned = 2
        };
        commit.TotalTagsAssigned.Should().Be(2);
        commit.CreatedTags.Should().ContainSingle();
        var suggest = new SuggestNoteTagsResponse { Suggestions = new[] { new TagSuggestionDto { Label = "csharp" } } };
        suggest.Suggestions.Should().HaveCount(1);
    }
}