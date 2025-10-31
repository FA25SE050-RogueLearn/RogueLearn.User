using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;

public record RevokeGuildRoleCommand(Guid GuildId, Guid MemberAuthUserId, GuildRole RoleToRevoke, Guid ActorAuthUserId, bool IsAdminOverride = false) : IRequest<Unit>;