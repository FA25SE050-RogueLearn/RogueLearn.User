using FluentValidation;

namespace RogueLearn.User.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
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