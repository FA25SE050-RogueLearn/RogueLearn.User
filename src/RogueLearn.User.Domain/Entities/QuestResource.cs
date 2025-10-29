using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_resources")]
public class QuestResource : BaseEntity
{
    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("resource_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ResourceType ResourceType { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("url")]
    public string? Url { get; set; }

    [Column("file_path")]
    public string? FilePath { get; set; }

    // Stored as JSONB; represented as string
    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}