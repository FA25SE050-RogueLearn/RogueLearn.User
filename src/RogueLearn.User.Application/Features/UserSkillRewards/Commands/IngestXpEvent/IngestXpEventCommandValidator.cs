using FluentValidation;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandValidator : AbstractValidator<IngestXpEventCommand>
{
    public IngestXpEventCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.SkillName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.SourceService).NotEmpty().MaximumLength(255);
        RuleFor(x => x.SourceId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("SourceId must be a non-empty GUID when provided");
        RuleFor(x => x.SourceType).MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.SourceType));
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}