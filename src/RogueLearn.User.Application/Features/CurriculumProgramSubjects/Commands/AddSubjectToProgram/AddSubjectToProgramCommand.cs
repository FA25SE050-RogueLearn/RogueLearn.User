using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;

public class AddSubjectToProgramCommand : IRequest<AddSubjectToProgramResponse>
{
    [JsonIgnore]
    public Guid ProgramId { get; set; }
    public Guid SubjectId { get; set; }
}

public class AddSubjectToProgramRequest
{
    public Guid SubjectId { get; set; }
}