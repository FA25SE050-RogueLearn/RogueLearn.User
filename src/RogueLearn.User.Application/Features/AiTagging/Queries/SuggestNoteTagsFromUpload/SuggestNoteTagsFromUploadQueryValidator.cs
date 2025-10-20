using FluentValidation;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQueryValidator : AbstractValidator<SuggestNoteTagsFromUploadQuery>
{
    public SuggestNoteTagsFromUploadQueryValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.FileContent).NotNull().Must(c => c.Length > 0).WithMessage("File content must not be empty.");
        RuleFor(x => x.MaxTags).InclusiveBetween(1, 20);
    }
}