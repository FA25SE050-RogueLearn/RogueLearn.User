using FluentValidation;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;

public class CreateGuildCommandValidator : AbstractValidator<CreateGuildCommand>
{
    public CreateGuildCommandValidator(IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
    {
        RuleFor(x => x.CreatorAuthUserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Privacy)
            .NotEmpty()
            .Must(p => string.Equals(p, "public", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "private", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Privacy must be either 'public' or 'private'.");

        RuleFor(x => x.MaxMembers)
            .GreaterThan(2)
            .MustAsync(async (cmd, max, ct) =>
            {
                var role = await roleRepository.GetByNameAsync("Verified Lecturer", ct);
                var roles = await userRoleRepository.GetRolesForUserAsync(cmd.CreatorAuthUserId, ct);
                var isVerifiedLecturer = role != null && roles.Any(r => r.RoleId == role.Id);
                var cap = isVerifiedLecturer ? 100 : 50;
                return max > 1 && max <= cap;
            })
            .WithMessage("Max guild members must be greater than 1 and not exceed your role-based cap.");
    }
}