using FluentValidation;

namespace RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;

public class DeleteNoteValidator : AbstractValidator<DeleteNoteCommand>
{
  public DeleteNoteValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty();

    RuleFor(x => x.AuthUserId)
      .NotEmpty();
  }
}