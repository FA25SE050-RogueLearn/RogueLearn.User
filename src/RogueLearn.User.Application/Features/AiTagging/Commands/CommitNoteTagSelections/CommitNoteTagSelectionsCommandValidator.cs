using FluentValidation;

namespace RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;

public class CommitNoteTagSelectionsCommandValidator : AbstractValidator<CommitNoteTagSelectionsCommand>
{
    public CommitNoteTagSelectionsCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.SelectedTagIds).NotNull();
        RuleFor(x => x.NewTagNames).NotNull();
        RuleForEach(x => x.NewTagNames).MaximumLength(100);
    }
}