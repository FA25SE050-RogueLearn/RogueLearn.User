using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMemberRoles;

public record GetGuildMemberRolesQuery(Guid GuildId, Guid MemberAuthUserId) : IRequest<IReadOnlyList<GuildRole>>;