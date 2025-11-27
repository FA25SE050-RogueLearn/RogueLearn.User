// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Commands/ImportSubjectFromText/ImportSubjectFromTextCommand.cs
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommand : IRequest<CreateSubjectResponse>
{
    public string RawText { get; set; } = string.Empty;

    // --- MODIFICATION: ADDED SEMESTER ---
    // This allows the admin to provide an explicit semester, overriding any value
    // extracted by the AI from the syllabus text.
    public int? Semester { get; set; }
}
