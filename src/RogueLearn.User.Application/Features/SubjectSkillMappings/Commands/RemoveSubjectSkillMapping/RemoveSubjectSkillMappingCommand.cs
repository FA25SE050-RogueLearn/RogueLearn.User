// RogueLearn.User/src/RogueLearn.User.Application/Features/SubjectSkillMappings/Commands/RemoveSubjectSkillMapping/RemoveSubjectSkillMappingCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.RemoveSubjectSkillMapping;

public class RemoveSubjectSkillMappingCommand : IRequest
{
    [JsonIgnore]
    public Guid SubjectId { get; set; }

    [JsonIgnore]
    public Guid SkillId { get; set; }
}