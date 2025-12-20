// src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestLineFromCurriculum/GenerateQuestLine.cs
using MediatR;
using RogueLearn.User.Application.Models; // Added for AcademicAnalysisReport

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

// generate high level quests of ALL subjects
public class GenerateQuestLine : IRequest<GenerateQuestLineResponse>
{
    public Guid AuthUserId { get; set; }

    // NEW: Pass the AI analysis report so we can adjust difficulty based on Persona/Weaknesses
    public AcademicAnalysisReport? AiAnalysisReport { get; set; }
}