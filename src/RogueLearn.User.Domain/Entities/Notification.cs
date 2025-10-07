using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("notifications")]
public class Notification : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("type")]
    public NotificationType Type { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
}