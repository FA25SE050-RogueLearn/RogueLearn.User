// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumProgramSubjects/Commands/RemoveSubjectFromProgram/RemoveSubjectFromProgramCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.RemoveSubjectFromProgram;

public class RemoveSubjectFromProgramCommand : IRequest
{
    [JsonIgnore]
    public Guid ProgramId { get; set; }
    [JsonIgnore]
    public Guid SubjectId { get; set; }
}