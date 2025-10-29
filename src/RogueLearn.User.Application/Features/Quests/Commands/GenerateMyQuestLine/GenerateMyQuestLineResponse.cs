// src/RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateMyQuestLine/GenerateMyQuestLineResponse.cs
namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateMyQuestLine;

public class GenerateMyQuestLineResponse
{
    public Guid LearningPathId { get; set; }
    public string LearningPathName { get; set; } = string.Empty;
    public int ChaptersCreated { get; set; }
    public int QuestsCreated { get; set; }
}