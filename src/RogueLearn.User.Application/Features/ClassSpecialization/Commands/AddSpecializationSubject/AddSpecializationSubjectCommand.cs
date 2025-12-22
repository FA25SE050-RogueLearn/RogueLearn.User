using MediatR;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;

public class AddSpecializationSubjectCommand : IRequest<SpecializationSubjectDto>
{
    [JsonIgnore]
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    // Removed PlaceholderSubjectCode
    public int Semester { get; set; }
}