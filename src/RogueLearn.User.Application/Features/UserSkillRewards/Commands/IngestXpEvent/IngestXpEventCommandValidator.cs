using FluentValidation;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandValidator : AbstractValidator<IngestXpEventCommand>
{
    public IngestXpEventCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.SkillId).NotEmpty().WithMessage("'Skill Id' must not be empty.");
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.SourceService).NotEmpty().MaximumLength(255);

        // Restore validation for SourceId
        RuleFor(x => x.SourceId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("SourceId must be a non-empty GUID when provided");

        RuleFor(x => x.Reason).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}