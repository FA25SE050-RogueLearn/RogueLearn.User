using FluentValidation;

namespace RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;

public class UpdateNoteValidator : AbstractValidator<UpdateNoteCommand>
{
  public UpdateNoteValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty();

    RuleFor(x => x.AuthUserId)
      .NotEmpty();

    RuleFor(x => x.Title)
      .NotEmpty()
      .MaximumLength(200);

    RuleFor(x => x.Content)
      .NotNull();
  }
}