using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("note_tags")]
public class NoteTag : BaseModel
{
    [Column("note_id")]
    public Guid NoteId { get; set; }

    [Column("tag_id")]
    public Guid TagId { get; set; }
}