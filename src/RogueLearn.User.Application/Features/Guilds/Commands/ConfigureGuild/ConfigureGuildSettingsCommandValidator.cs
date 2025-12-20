using FluentValidation;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;

public class ConfigureGuildSettingsCommandValidator : AbstractValidator<ConfigureGuildSettingsCommand>
{
    public ConfigureGuildSettingsCommandValidator(IGuildRepository guildRepository, IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
    {
        RuleFor(x => x.GuildId)
            .NotEmpty();

        RuleFor(x => x.ActorAuthUserId)
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
            .CustomAsync(async (max, context, ct) =>
            {
                var cmd = (ConfigureGuildSettingsCommand)context.InstanceToValidate;
                var guild = await guildRepository.GetByIdAsync(cmd.GuildId, ct);
                if (guild == null)
                {
                    context.AddFailure("GuildId", "Guild not found.");
                    return;
                }

                var role = await roleRepository.GetByNameAsync("Verified Lecturer", ct);
                var roles = await userRoleRepository.GetRolesForUserAsync(cmd.ActorAuthUserId, ct);
                var isVerifiedLecturer = role != null && roles.Any(r => r.RoleId == role.Id);
                var cap = isVerifiedLecturer ? 100 : 50;

                var isValid = max > guild.CurrentMemberCount && max <= cap;
                if (!isValid)
                {
                    context.AddFailure("MaxMembers", $"Max members must be greater than current members ({guild.CurrentMemberCount}) and cannot exceed {cap} for your role.");
                }
            });
    }
}