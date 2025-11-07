// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/SuggestSkillsFromSyllabus/SuggestSkillsFromSyllabusResponse.cs
namespace RogueLearn.User.Application.Features.AdminCurriculum.SuggestSkillsFromSyllabus;

public class SuggestSkillsFromSyllabusResponse
{
    public string Message { get; set; } = string.Empty;
    public List<SuggestedSkillDto> SuggestedSkills { get; set; } = new();
}

public class SuggestedSkillDto
{
    public string Name { get; set; } = string.Empty;
    public bool ExistsInCatalog { get; set; }
}
