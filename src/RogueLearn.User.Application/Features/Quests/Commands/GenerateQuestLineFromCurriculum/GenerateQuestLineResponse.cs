// src/RogueLearn.User/Application/Features/Quests/Commands/GenerateQuestLineFromCurriculum/GenerateQuestLineResponse.cs
namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

public class GenerateQuestLineResponse
{
    public Guid LearningPathId { get; set; }
    public int ChaptersCreated { get; set; }
    public int QuestsCreated { get; set; }
}