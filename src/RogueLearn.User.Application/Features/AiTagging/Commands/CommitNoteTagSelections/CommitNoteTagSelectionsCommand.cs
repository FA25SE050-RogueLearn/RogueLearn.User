using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;

public class CommitNoteTagSelectionsCommand : IRequest<CommitNoteTagSelectionsResponse>
{
    public Guid AuthUserId { get; set; }
    public Guid NoteId { get; set; }
    public List<Guid> SelectedTagIds { get; set; } = new();
    public List<string> NewTagNames { get; set; } = new();
}