// RogueLearn.User/src/RogueLearn.User.Application/Features/UserSkillRewards/Commands/IngestXpEvent/IngestXpEventCommandValidator.cs
using FluentValidation;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandValidator : AbstractValidator<IngestXpEventCommand>
{
    public IngestXpEventCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        // MODIFICATION: Validate the SkillId, which is now the primary identifier.
        RuleFor(x => x.SkillId).NotEmpty().WithMessage("'Skill Id' must not be empty.");
        // MODIFICATION: SkillName is no longer a required input, as it will be looked up by the handler.
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.SourceService).NotEmpty().MaximumLength(255);
        RuleFor(x => x.SourceId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("SourceId must be a non-empty GUID when provided");
        RuleFor(x => x.SourceType).MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.SourceType));
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}