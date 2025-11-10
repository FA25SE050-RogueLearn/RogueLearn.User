namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNote;

public class CreateNoteResponse
{
  public Guid Id { get; set; }
  public Guid AuthUserId { get; set; }
  public string Title { get; set; } = string.Empty;
  public object? Content { get; set; }
  public bool IsPublic { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }
}