using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Commands.StartQuest;

public record StartQuestCommand(Guid QuestId, Guid AuthUserId) : IRequest<StartQuestResponse>;