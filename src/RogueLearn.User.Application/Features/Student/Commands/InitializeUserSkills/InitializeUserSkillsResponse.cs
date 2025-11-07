// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/InitializeUserSkills/InitializeUserSkillsResponse.cs
namespace RogueLearn.User.Application.Features.Student.Commands.InitializeUserSkills;

public class InitializeUserSkillsResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;

    // MODIFIED: These properties are no longer relevant in the deterministic model.
    // The system now links existing skills, it doesn't extract or create them from a syllabus.
    // public int TotalSkillsExtracted { get; set; }
    // public List<string> MissingFromCatalog { get; set; } = new();

    public int SkillsInitialized { get; set; }
    public int SkillsSkipped { get; set; }
}