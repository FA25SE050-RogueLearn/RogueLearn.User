using FluentAssertions;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestById;

public class QuestStepDtoTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var dto = new QuestStepDto();
        dto.Id.Should().Be(Guid.Empty);
        dto.StepNumber.Should().Be(0);
        dto.Title.Should().Be(string.Empty);
        dto.Description.Should().Be(string.Empty);
        dto.StepType.Should().Be(string.Empty);
        dto.ExperiencePoints.Should().Be(0);
        dto.Content.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var dto = new QuestStepDto
        {
            Id = Guid.NewGuid(),
            StepNumber = 2,
            Title = "Quiz",
            Description = "Chapter 1 Quiz",
            StepType = "Quiz",
            ExperiencePoints = 100,
            Content = new { questions = 10 }
        };

        dto.StepNumber.Should().Be(2);
        dto.StepType.Should().Be("Quiz");
        dto.ExperiencePoints.Should().Be(100);
        dto.Content.Should().NotBeNull();
    }
}