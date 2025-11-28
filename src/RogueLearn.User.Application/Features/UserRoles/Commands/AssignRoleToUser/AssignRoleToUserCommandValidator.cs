using FluentValidation;

namespace RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(x => x.AuthUserId)
            .NotEmpty()
            .WithMessage("Auth user ID is required");

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required");
    }
}