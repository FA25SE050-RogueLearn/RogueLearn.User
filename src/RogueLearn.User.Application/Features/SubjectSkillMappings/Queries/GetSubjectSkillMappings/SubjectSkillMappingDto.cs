// RogueLearn.User/src/RogueLearn.User.Application/Features/SubjectSkillMappings/Queries/GetSubjectSkillMappings/SubjectSkillMappingDto.cs
namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;

public class SubjectSkillMappingDto
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public decimal RelevanceWeight { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}