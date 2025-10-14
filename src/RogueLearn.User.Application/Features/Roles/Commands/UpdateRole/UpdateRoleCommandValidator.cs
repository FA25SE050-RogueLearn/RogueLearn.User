using FluentValidation;

namespace RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Role ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required.")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters.")
            .Matches("^[a-zA-Z0-9_\\s-]+$")
            .WithMessage("Role name can only contain letters, numbers, spaces, underscores, and hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Role description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}