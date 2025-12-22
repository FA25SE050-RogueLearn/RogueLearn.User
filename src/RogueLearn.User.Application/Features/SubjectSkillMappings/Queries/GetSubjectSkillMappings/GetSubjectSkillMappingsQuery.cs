using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;

public class GetSubjectSkillMappingsQuery : IRequest<List<SubjectSkillMappingDto>>
{
    [JsonIgnore]
    public Guid SubjectId { get; set; }
}