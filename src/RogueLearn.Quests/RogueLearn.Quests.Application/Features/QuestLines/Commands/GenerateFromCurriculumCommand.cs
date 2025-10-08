// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Application/Features/QuestLines/Commands/GenerateFromCurriculumCommand.cs
using MediatR;

namespace RogueLearn.Quests.Application.Features.QuestLines.Commands;

public class GenerateFromCurriculumCommand : IRequest<Guid>
{
	public Guid UserId { get; set; }
	public Guid CurriculumVersionId { get; set; }
}