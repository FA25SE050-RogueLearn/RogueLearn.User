using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("skill_dependencies")]
public class SkillDependency : BaseEntity
{
    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("prerequisite_skill_id")]
    public Guid PrerequisiteSkillId { get; set; }

    [Column("relationship_type")]
    public string RelationshipType { get; set; } = "Prerequisite";
}