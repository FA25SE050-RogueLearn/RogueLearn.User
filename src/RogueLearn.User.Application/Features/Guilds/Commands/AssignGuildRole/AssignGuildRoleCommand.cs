using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;

public record AssignGuildRoleCommand(Guid GuildId, Guid MemberAuthUserId, GuildRole RoleToAssign, Guid ActorAuthUserId, bool IsAdminOverride = false) : IRequest<Unit>;