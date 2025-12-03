using FluentAssertions;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNote;

public class CreateNoteValidatorTests
{
    [Fact]
    public void Invalid_WhenMissingFields()
    {
        var validator = new CreateNoteValidator();
        var cmd = new CreateNoteCommand { AuthUserId = Guid.Empty, Title = "", Content = null };
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_WhenFieldsProvided()
    {
        var validator = new CreateNoteValidator();
        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "T", Content = new object() };
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}