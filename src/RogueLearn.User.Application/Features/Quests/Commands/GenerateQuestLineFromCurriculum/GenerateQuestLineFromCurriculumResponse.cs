// src/RogueLearn.User/Application/Features/Quests/Commands/GenerateQuestLineFromCurriculum/GenerateQuestLineFromCurriculumResponse.cs
namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

public class GenerateQuestLineFromCurriculumResponse
{
    public Guid LearningPathId { get; set; }
    public int ChaptersCreated { get; set; }
    public int QuestsCreated { get; set; }
}