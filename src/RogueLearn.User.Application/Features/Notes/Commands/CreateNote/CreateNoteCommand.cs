using MediatR;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNote;

public class CreateNoteCommand : IRequest<CreateNoteResponse>
{
  public Guid AuthUserId { get; set; }
  public string Title { get; set; } = string.Empty;
  public string? Content { get; set; }
  public bool IsPublic { get; set; } = false;

  // Optional relationship assignments on create
  public List<Guid>? TagIds { get; set; }
  public List<Guid>? SkillIds { get; set; }
  public List<Guid>? QuestIds { get; set; }
}