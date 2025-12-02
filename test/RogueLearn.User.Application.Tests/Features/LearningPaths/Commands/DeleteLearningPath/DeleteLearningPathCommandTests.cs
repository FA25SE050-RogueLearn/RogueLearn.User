using FluentAssertions;
using RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Commands.DeleteLearningPath;

public class DeleteLearningPathCommandTests
{
    [Fact]
    public void Command_SetsId()
    {
        var id = Guid.NewGuid();
        var cmd = new DeleteLearningPathCommand { Id = id };
        cmd.Id.Should().Be(id);
    }
}