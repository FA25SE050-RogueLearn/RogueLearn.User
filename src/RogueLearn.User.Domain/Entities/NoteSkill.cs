using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("note_skills")]
public class NoteSkill : BaseModel
{
    [Column("note_id")]
    public Guid NoteId { get; set; }

    [Column("skill_id")]
    public Guid SkillId { get; set; }
}