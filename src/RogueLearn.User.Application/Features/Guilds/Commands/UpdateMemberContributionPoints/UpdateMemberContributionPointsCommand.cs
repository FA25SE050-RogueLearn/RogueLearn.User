using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.UpdateMemberContributionPoints;

public record UpdateMemberContributionPointsCommand(Guid GuildId, Guid MemberAuthUserId, int PointsDelta) : IRequest<Unit>;