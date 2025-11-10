// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordResponse.cs
namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    // MODIFIED: This ID will now be populated by the newly triggered quest generation process.
    public Guid LearningPathId { get; set; }
    public int SubjectsProcessed { get; set; }
    public int QuestsGenerated { get; set; }
    public double CalculatedGpa { get; set; }
}