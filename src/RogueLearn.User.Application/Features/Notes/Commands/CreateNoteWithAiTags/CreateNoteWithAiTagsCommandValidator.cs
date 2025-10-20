using FluentValidation;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;

public class CreateNoteWithAiTagsCommandValidator : AbstractValidator<CreateNoteWithAiTagsCommand>
{
    public CreateNoteWithAiTagsCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.MaxTags).InclusiveBetween(1, 20);
        RuleFor(x => x.Title).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x)
            .Must(cmd => !string.IsNullOrWhiteSpace(cmd.RawText) || (cmd.FileContent is { Length: > 0 }))
            .WithMessage("Either rawText or fileContent must be provided.");
    }
}