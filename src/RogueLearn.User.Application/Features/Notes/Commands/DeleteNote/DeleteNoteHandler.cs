using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;

public class DeleteNoteHandler : IRequestHandler<DeleteNoteCommand>
{
  private readonly INoteRepository _noteRepository;

  public DeleteNoteHandler(INoteRepository noteRepository)
  {
    _noteRepository = noteRepository;
  }

  public async Task Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
  {
    var note = await _noteRepository.GetByIdAsync(request.Id, cancellationToken);
    if (note is null)
    {
      throw new NotFoundException($"Note with ID {request.Id} not found.");
    }

    if (note.AuthUserId != request.AuthUserId)
    {
      throw new ForbiddenException("You are not allowed to delete this note.");
    }

    await _noteRepository.DeleteAsync(request.Id, cancellationToken);
    // Linked entities in note_* tables are ON DELETE CASCADE per schema.
  }
}