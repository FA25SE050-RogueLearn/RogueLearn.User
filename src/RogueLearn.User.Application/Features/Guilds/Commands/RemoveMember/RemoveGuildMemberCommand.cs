using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;

public record RemoveGuildMemberCommand(Guid GuildId, Guid MemberId, string? Reason) : IRequest<Unit>;