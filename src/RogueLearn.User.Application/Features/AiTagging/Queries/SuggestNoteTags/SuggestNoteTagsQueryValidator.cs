using FluentValidation;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;

public class SuggestNoteTagsQueryValidator : AbstractValidator<SuggestNoteTagsQuery>
{
    public SuggestNoteTagsQueryValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.RawText) || x.NoteId.HasValue)
            .WithMessage("Either rawText or noteId must be provided.");
        RuleFor(x => x.MaxTags).InclusiveBetween(1, 20);
    }
}