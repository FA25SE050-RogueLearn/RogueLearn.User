namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;

public class AddSubjectSkillMappingResponse
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid SkillId { get; set; }
    public decimal RelevanceWeight { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}