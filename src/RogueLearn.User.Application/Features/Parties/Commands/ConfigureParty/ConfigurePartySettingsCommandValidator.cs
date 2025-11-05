using FluentValidation;

namespace RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;

public class ConfigurePartySettingsCommandValidator : AbstractValidator<ConfigurePartySettingsCommand>
{
    public ConfigurePartySettingsCommandValidator()
    {
        RuleFor(x => x.PartyId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.MaxMembers)
            .GreaterThan(0)
            .LessThanOrEqualTo(8);

        RuleFor(x => x.Privacy)
            .NotEmpty()
            .Must(p => string.Equals(p, "public", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "private", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Privacy must be either 'public' or 'private'.");

        // Description is optional; you can enforce length limits if desired:
        // RuleFor(x => x.Description).MaximumLength(1000);
    }
}