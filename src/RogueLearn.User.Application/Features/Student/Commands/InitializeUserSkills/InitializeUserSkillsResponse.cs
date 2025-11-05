using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Features.Student.Commands.InitializeUserSkills;

public class InitializeUserSkillsResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalSkillsExtracted { get; set; }
    public int SkillsInitialized { get; set; }
    public int SkillsSkipped { get; set; }
    public List<string> MissingFromCatalog { get; set; } = new();
}
