using System;
using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.AiTagging.Commands.CommitNoteTagSelections;

public class CommitNoteTagSelectionsCommandValidatorTests
{
    [Fact]
    public void Validate_Passes_For_Valid()
    {
        var v = new CommitNoteTagSelectionsCommandValidator();
        var cmd = new CommitNoteTagSelectionsCommand
        {
            AuthUserId = Guid.NewGuid(),
            NoteId = Guid.NewGuid(),
            SelectedTagIds = new List<Guid> { Guid.NewGuid() },
            NewTagNames = new List<string> { "TagA" }
        };
        var result = v.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_For_Missing_Ids()
    {
        var v = new CommitNoteTagSelectionsCommandValidator();
        var cmd = new CommitNoteTagSelectionsCommand
        {
            AuthUserId = Guid.Empty,
            NoteId = Guid.Empty,
            SelectedTagIds = null!,
            NewTagNames = null!
        };
        var result = v.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_For_Long_TagName()
    {
        var v = new CommitNoteTagSelectionsCommandValidator();
        var cmd = new CommitNoteTagSelectionsCommand
        {
            AuthUserId = Guid.NewGuid(),
            NoteId = Guid.NewGuid(),
            SelectedTagIds = new List<Guid>(),
            NewTagNames = new List<string> { new string('x', 101) }
        };
        var result = v.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}