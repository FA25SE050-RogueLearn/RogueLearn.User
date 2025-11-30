using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("achievements")]
public class Achievement : BaseEntity
{
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    // New rule fields
    [Column("rule_type")]
    public string? RuleType { get; set; }

    [Column("rule_config")]
    public Dictionary<string, object>? RuleConfig { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("icon_url")]
    public string? IconUrl { get; set; }

    [Column("version")]
    public int? Version { get; set; } = 1;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("source_service")]
    public string SourceService { get; set; } = string.Empty;

    [Column("merit_points_reward")]
    public int? MeritPointsReward { get; set; }

    [Column("contribution_points_reward")]
    public int? ContributionPointsReward { get; set; }

    [Column("is_medal")]
    public bool IsMedal { get; set; } = true;
}
