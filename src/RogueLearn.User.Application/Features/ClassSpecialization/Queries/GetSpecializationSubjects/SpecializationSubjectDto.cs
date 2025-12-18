// RogueLearn.User/src/RogueLearn.User.Application/Features/ClassSpecialization/Queries/GetSpecializationSubjects/SpecializationSubjectDto.cs
namespace RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;

public class SpecializationSubjectDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    // Removed PlaceholderSubjectCode
    public int Semester { get; set; }

    // NOTE: In the future, you should add these to the DTO and populate them via JOIN
    // so the Frontend doesn't have to fetch them separately. 
    // For now, I am keeping the DTO minimal to match your current refactor.
    // public string SubjectCode { get; set; }
    // public string SubjectName { get; set; }
}