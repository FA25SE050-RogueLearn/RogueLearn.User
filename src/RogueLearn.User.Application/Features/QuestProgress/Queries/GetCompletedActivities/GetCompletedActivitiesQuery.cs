// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetCompletedActivities/GetCompletedActivitiesQuery.cs
using MediatR;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;

public class GetCompletedActivitiesQuery : IRequest<CompletedActivitiesDto>
{
    public Guid AuthUserId { get; set; }
    public Guid QuestId { get; set; }
    public Guid StepId { get; set; }
}

public class CompletedActivitiesDto
{
    public Guid StepId { get; set; }
    public List<ActivityProgressDto> Activities { get; set; } = new();
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
}

public class ActivityProgressDto
{
    public Guid ActivityId { get; set; }
    public string ActivityType { get; set; } // Reading, Quiz, KnowledgeCheck, Coding
    public bool IsCompleted { get; set; }
    public string? Title { get; set; }
    public int ExperiencePoints { get; set; }
    public Guid? SkillId { get; set; }
}
