using MediatR;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommand : IRequest<IngestXpEventResponse>
{
    public Guid AuthUserId { get; set; }

    // This input string will be parsed into the SourceService enum
    public string SourceService { get; set; } = string.Empty;

    public Guid? SourceId { get; set; }

    public Guid? SkillId { get; set; }
    public int Points { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
}