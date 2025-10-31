using FluentValidation;

namespace RogueLearn.User.Application.Features.Parties.Commands.CreateParty;

public class CreatePartyCommandValidator : AbstractValidator<CreatePartyCommand>
{
    public CreatePartyCommandValidator()
    {
        RuleFor(x => x.CreatorAuthUserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.MaxMembers)
            .GreaterThan(0)
            .LessThanOrEqualTo(8);
    }
}