// RogueLearn.User/src/RogueLearn.User.Application/Features/ClassSpecialization/Queries/GetSpecializationSubjects/SpecializationSubjectDto.cs
namespace RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;

public class SpecializationSubjectDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public string PlaceholderSubjectCode { get; set; } = string.Empty;
    public int Semester { get; set; }
}