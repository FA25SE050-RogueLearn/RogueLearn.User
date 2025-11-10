namespace RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;

public class NoteDto
{
  public Guid Id { get; set; }
  public Guid AuthUserId { get; set; }
  public string Title { get; set; } = string.Empty;
  public object? Content { get; set; }
  public bool IsPublic { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }

  public List<Guid> TagIds { get; set; } = new();
  public List<Guid> SkillIds { get; set; } = new();
  public List<Guid> QuestIds { get; set; } = new();
}