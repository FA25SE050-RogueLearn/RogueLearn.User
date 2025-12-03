using FluentAssertions;
using RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Commands.DeleteLearningPath;

public class DeleteLearningPathCommandValidatorTests
{
    [Fact]
    public void Invalid_WhenIdEmpty()
    {
        var validator = new DeleteLearningPathCommandValidator();
        var result = validator.Validate(new DeleteLearningPathCommand { Id = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Any(e => e.PropertyName == nameof(DeleteLearningPathCommand.Id)).Should().BeTrue();
    }

    [Fact]
    public void Valid_WhenIdProvided()
    {
        var validator = new DeleteLearningPathCommandValidator();
        var result = validator.Validate(new DeleteLearningPathCommand { Id = Guid.NewGuid() });
        result.IsValid.Should().BeTrue();
    }
}