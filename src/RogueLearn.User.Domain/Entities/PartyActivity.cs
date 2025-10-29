using System.Text.Json.Serialization;
using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("party_activities")]
public class PartyActivity : BaseEntity
{
    [Column("party_id")]
    public Guid PartyId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Column("activity_type")]
    public ActivityType ActivityType { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("quest_id")]
    public Guid? QuestId { get; set; }

    [Column("meeting_id")]
    public Guid? MeetingId { get; set; }

    [Column("started_at")]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("experience_points_earned")]
    public int ExperiencePointsEarned { get; set; } = 0;

    [Column("participants")]
    public Guid[] Participants { get; set; } = Array.Empty<Guid>();

    [Column("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}