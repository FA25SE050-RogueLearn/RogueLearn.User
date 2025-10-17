using MediatR;

namespace RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;

public class UpdateNoteCommand : IRequest<UpdateNoteResponse>
{
  public Guid Id { get; set; }
  public Guid AuthUserId { get; set; }
  public string Title { get; set; } = string.Empty;
  public string? Content { get; set; }
  public bool IsPublic { get; set; } = false;

  // Optional full relationship set to apply on update
  public List<Guid>? TagIds { get; set; }
  public List<Guid>? SkillIds { get; set; }
  public List<Guid>? QuestIds { get; set; }
}