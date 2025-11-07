// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/CreateAndMapSkills/CreateAndMapSkillsResponse.cs
namespace RogueLearn.User.Application.Features.AdminCurriculum.CreateAndMapSkills;

public class CreateAndMapSkillsResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> SkillsCreated { get; set; } = new();
    public List<string> SkillsReused { get; set; } = new();
    public List<string> MappingsCreated { get; set; } = new();
    public List<string> MappingsExisted { get; set; } = new();
}