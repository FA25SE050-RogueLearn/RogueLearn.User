using FluentValidation;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNote;

public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
  public CreateNoteValidator()
  {
    RuleFor(x => x.AuthUserId)
      .NotEmpty();

    RuleFor(x => x.Title)
      .NotEmpty()
      .MaximumLength(200);

    RuleFor(x => x.Content)
      .NotNull();
  }
}