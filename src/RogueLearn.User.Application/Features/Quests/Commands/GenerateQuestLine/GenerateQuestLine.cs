// src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestLineFromCurriculum/GenerateQuestLine.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

// generate high level quests of ALL subjects
public class GenerateQuestLine : IRequest<GenerateQuestLineResponse>
{
    public Guid AuthUserId { get; set; }
}