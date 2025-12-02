using FluentAssertions;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;

namespace RogueLearn.User.Application.Tests.Features.QuestProgress.Queries.GetCompletedActivities;

public class GetCompletedActivitiesResponseTests
{
    [Fact]
    public void Response_SetsFields()
    {
        var r = new GetCompletedActivitiesResponse
        {
            StepId = Guid.NewGuid(),
            Activities = new List<ActivityProgressDto> { new ActivityProgressDto { ActivityId = Guid.NewGuid(), IsCompleted = true, ActivityType = "Reading" } },
            CompletedCount = 1,
            TotalCount = 1
        };
        r.Activities.Should().HaveCount(1);
    }
}