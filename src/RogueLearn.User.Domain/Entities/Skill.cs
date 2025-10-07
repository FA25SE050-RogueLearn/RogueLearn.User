using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("skills")]
public class Skill : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("domain")]
    public string? Domain { get; set; }

    [Column("tier")]
    public int Tier { get; set; } = 1;

    [Column("description")]
    public string? Description { get; set; }
}