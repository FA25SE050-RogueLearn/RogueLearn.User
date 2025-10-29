using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("skill_dependencies")]
public class SkillDependency : BaseEntity
{
    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("prerequisite_skill_id")]
    public Guid PrerequisiteSkillId { get; set; }

  [Column("relationship_type")]
  [JsonConverter(typeof(StringEnumConverter))]
  public SkillRelationshipType RelationshipType { get; set; } = SkillRelationshipType.Prerequisite;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}