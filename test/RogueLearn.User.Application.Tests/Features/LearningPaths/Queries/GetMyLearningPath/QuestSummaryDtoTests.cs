using FluentAssertions;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Queries.GetMyLearningPath;

public class QuestSummaryDtoTests
{
    [Fact]
    public void SequenceOrder_Default_Is_Zero()
    {
        new QuestSummaryDto().SequenceOrder.Should().Be(0);
    }

    [Fact]
    public void SequenceOrder_Settable()
    {
        var dto = new QuestSummaryDto { SequenceOrder = 3 };
        dto.SequenceOrder.Should().Be(3);
    }
}

