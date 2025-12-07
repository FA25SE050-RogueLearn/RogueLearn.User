// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/StartQuest/StartQuestCommand.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Commands.StartQuest;

public record StartQuestCommand(Guid QuestId, Guid AuthUserId) : IRequest<StartQuestResponse>;