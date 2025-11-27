using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextRequest
{
    public string RawText { get; set; } = string.Empty;
    public int? Semester { get; set; }
}
