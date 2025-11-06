// RogueLearn.User/src/RogueLearn.User.Application/Features/Skills/Queries/GetSkillTree/GetSkillTreeQuery.cs
using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillTree;

public class GetSkillTreeQuery : IRequest<SkillTreeDto>
{
    public Guid AuthUserId { get; set; }
}

public class SkillTreeDto
{
    public List<SkillNodeDto> Nodes { get; set; } = new();
    public List<SkillDependencyDto> Dependencies { get; set; } = new();
}

public class SkillNodeDto
{
    public Guid SkillId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Description { get; set; }
    public int Tier { get; set; }
    public int UserLevel { get; set; }
    public int UserExperiencePoints { get; set; }
}

public class SkillDependencyDto
{
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public SkillRelationshipType RelationshipType { get; set; }
}