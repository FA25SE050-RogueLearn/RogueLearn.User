using RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkillDetail;

public class SkillQuestDtoTests
{
    [Fact]
    public void SkillQuestDto_ConstructsAndSerializes()
    {
        var dto = new SkillQuestDto
        {
            QuestId = Guid.NewGuid(),
            Title = "Quest1",
            XpReward = 100,
            Type = "QuestType1"
        };
        Assert.Equal(dto.QuestId, dto.QuestId);
        Assert.Equal(dto.Title, dto.Title);
        Assert.Equal(dto.XpReward, dto.XpReward);
        Assert.Equal(dto.Type, dto.Type);
    }
}