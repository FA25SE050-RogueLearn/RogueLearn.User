using FluentValidation;

namespace RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesQueryValidator : AbstractValidator<GetUserRolesQuery>
{
    public GetUserRolesQueryValidator()
    {
        RuleFor(x => x.AuthUserId)
            .NotEmpty()
            .WithMessage("Auth user ID is required.");
    }
}