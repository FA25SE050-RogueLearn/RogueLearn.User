// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Application/DTOs/CurriculumStructureDto.cs
namespace RogueLearn.Quests.Application.DTOs;

// IMPORTANT: This DTO must match the structure provided by the UserService API.
public class CurriculumStructureDto
{
	public Guid SubjectId { get; set; }
	public string SubjectName { get; set; } = string.Empty;
	public int TermNumber { get; set; }
}