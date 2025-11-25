using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.GrantGuildAchievement;

public record GrantGuildAchievementCommand(Guid GuildId, string AchievementKey) : IRequest<Unit>;