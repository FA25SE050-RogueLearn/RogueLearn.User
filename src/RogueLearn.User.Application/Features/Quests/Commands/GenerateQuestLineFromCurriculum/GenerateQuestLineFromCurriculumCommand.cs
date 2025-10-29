// src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestLineFromCurriculum/GenerateQuestLineFromCurriculumCommand.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

public class GenerateQuestLineFromCurriculumCommand : IRequest<GenerateQuestLineFromCurriculumResponse>
{
    public Guid CurriculumVersionId { get; set; }
    public Guid AuthUserId { get; set; }
}