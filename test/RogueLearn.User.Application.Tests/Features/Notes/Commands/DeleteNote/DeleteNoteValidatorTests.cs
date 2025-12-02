using FluentAssertions;
using RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.DeleteNote;

public class DeleteNoteValidatorTests
{
    [Fact]
    public void Invalid_WhenMissingIds()
    {
        var validator = new DeleteNoteValidator();
        var cmd = new DeleteNoteCommand { Id = Guid.Empty, AuthUserId = Guid.Empty };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_WhenIdsProvided()
    {
        var validator = new DeleteNoteValidator();
        var cmd = new DeleteNoteCommand { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid() };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeTrue();
    }
}