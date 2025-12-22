using MediatR;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommand : IRequest<IngestXpEventResponse>
{
    public Guid AuthUserId { get; set; }

    // Source identifiers for idempotency and traceability
    public string SourceService { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }

    // Skill & XP payload
    // Add SkillId as the primary identifier.
    public Guid? SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty; // Retain for logging and display purposes.
    public int Points { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
}
