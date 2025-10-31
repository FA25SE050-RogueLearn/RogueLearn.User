using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildMembers;

public record GetGuildMembersQuery(Guid GuildId) : IRequest<IEnumerable<GuildMemberDto>>;