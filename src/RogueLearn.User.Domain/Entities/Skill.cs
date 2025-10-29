using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("skills")]
public class Skill : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("domain")]
    public string? Domain { get; set; }

  [Column("tier")]
  [JsonConverter(typeof(StringEnumConverter))]
  public SkillTierLevel Tier { get; set; } = SkillTierLevel.Foundation;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}