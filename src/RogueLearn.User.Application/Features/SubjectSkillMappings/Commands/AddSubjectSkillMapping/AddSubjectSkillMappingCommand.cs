// RogueLearn.User/src/RogueLearn.User.Application/Features/SubjectSkillMappings/Commands/AddSubjectSkillMapping/AddSubjectSkillMappingCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;

public class AddSubjectSkillMappingCommand : IRequest<AddSubjectSkillMappingResponse>
{
    [JsonIgnore]
    public Guid SubjectId { get; set; }
    public Guid SkillId { get; set; }
    public decimal RelevanceWeight { get; set; } = 1.00m;
}