using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("note_quests")]
public class NoteQuest : BaseModel
{
    [Column("note_id")]
    public Guid NoteId { get; set; }

    [Column("quest_id")]
    public Guid QuestId { get; set; }
}
