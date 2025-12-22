using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;

public class SkillDetailDto
{
    // Header & Metadata
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // User Progress
    public int CurrentLevel { get; set; }
    public int CurrentXp { get; set; }
    public int XpForNextLevel { get; set; } // e.g., 1000
    public int XpProgressInLevel { get; set; } // e.g., 230
    public double ProgressPercentage { get; set; } // e.g., 45.5

    // Relationships
    public List<DependencyStatusDto> Prerequisites { get; set; } = new();
    public List<DependencyStatusDto> Unlocks { get; set; } = new();

    // Learning Path (Quests linked to this skill via Subjects)
    public List<SkillQuestDto> LearningPath { get; set; } = new();
}

public class DependencyStatusDto
{
    public Guid SkillId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsMet { get; set; } // For prereqs: is it completed? For unlocks: is it available?
    public int UserLevel { get; set; }
    public string StatusLabel { get; set; } = string.Empty; // "100% Complete" or "Requires Functions completion"
}

public class SkillQuestDto
{
    public Guid QuestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int XpReward { get; set; }
    public string Type { get; set; } = string.Empty; // "Quest" or "Boss Fight"
}