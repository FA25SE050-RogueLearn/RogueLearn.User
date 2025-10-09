using FluentValidation;

namespace RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesQueryValidator : AbstractValidator<GetUserRolesQuery>
{
    public GetUserRolesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}