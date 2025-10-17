namespace RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;

public class UpdateNoteResponse
{
  public Guid Id { get; set; }
  public Guid AuthUserId { get; set; }
  public string Title { get; set; } = string.Empty;
  public string? Content { get; set; }
  public bool IsPublic { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }
}