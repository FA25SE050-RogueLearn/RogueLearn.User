using FluentAssertions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.GenerateQuestSteps;

public class GeneratedQuestStepDtoTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var dto = new GeneratedQuestStepDto();
        dto.Id.Should().Be(Guid.Empty);
        dto.QuestId.Should().Be(Guid.Empty);
        dto.StepNumber.Should().Be(0);
        dto.Title.Should().Be(string.Empty);
        dto.Description.Should().Be(string.Empty);
        dto.Content.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var dto = new GeneratedQuestStepDto
        {
            Id = Guid.NewGuid(),
            QuestId = Guid.NewGuid(),
            StepNumber = 1,
            Title = "Read Chapter 1",
            Description = "Introduction",
            StepType = StepType.Quiz,
            Content = new { text = "What is a variable?" }
        };

        dto.StepType.Should().Be(StepType.Quiz);
        dto.Content.Should().NotBeNull();
    }
}