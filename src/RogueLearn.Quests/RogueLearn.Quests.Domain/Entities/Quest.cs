// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Domain/Entities/Quest.cs
using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.Quests.Domain.Entities;

// Represents the 'quests' table in the database.
[Table("quests")]
public class Quest : BaseEntity
{
	[Column("title")]
	public string Title { get; set; } = string.Empty;

	[Column("description")]
	public string Description { get; set; } = string.Empty;

	// The database uses an ENUM type 'quest_type'. We'll map it to a C# enum.
	[Column("quest_type")]
	public QuestType QuestType { get; set; }

	// The database uses an ENUM type 'difficulty_level'. We'll map it to a C# enum.
	[Column("difficulty_level")]
	public DifficultyLevel DifficultyLevel { get; set; }

	[Column("estimated_duration_minutes")]
	public int? EstimatedDurationMinutes { get; set; }

	[Column("experience_points_reward")]
	public int ExperiencePointsReward { get; set; }

	[Column("skill_tags")]
	public string[]? SkillTags { get; set; }

	[Column("prerequisites")]
	public Guid[]? Prerequisites { get; set; }

	[Column("subject_id")]
	public Guid? SubjectId { get; set; } // Reference to subjects table in User Service

	[Column("is_active")]
	public bool IsActive { get; set; } = true;

	[Column("created_by")]
	public Guid CreatedBy { get; set; } // Reference to user_profiles.auth_user_id in User Service
}

// Define the enums used by the Quest entity.
public enum QuestType
{
	Tutorial,
	Practice,
	Challenge,
	Project,
	Assessment,
	Exploration
}

public enum DifficultyLevel
{
	Beginner,
	Intermediate,
	Advanced,
	Expert
}