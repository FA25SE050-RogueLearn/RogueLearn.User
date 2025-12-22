namespace RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;

public class AddSubjectToProgramResponse
{
    public Guid ProgramId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}