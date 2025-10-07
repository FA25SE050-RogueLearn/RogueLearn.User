using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("achievements")]
public class Achievement : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("icon_url")]
    public string? IconUrl { get; set; }

    [Column("source_service")]
    public string SourceService { get; set; } = string.Empty;
}