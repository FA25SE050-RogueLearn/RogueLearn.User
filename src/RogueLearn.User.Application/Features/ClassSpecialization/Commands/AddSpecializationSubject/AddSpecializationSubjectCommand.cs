// RogueLearn.User/src/RogueLearn.User.Application/Features/ClassSpecialization/Commands/AddSpecializationSubject/AddSpecializationSubjectCommand.cs
using MediatR;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;

public class AddSpecializationSubjectCommand : IRequest<SpecializationSubjectDto>
{
    [JsonIgnore]
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public string PlaceholderSubjectCode { get; set; } = string.Empty;
    public int Semester { get; set; }
}