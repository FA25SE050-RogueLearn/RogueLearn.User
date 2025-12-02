using FluentAssertions;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.UpdateNote;

public class UpdateNoteValidatorTests
{
    [Fact]
    public void Invalid_WhenMissingIds()
    {
        var validator = new UpdateNoteValidator();
        var cmd = new UpdateNoteCommand { Id = Guid.Empty, AuthUserId = Guid.Empty, Title = "t" };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_WhenIdsProvided()
    {
        var validator = new UpdateNoteValidator();
        var cmd = new UpdateNoteCommand { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Title = "t" };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeTrue();
    }
}