// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordCommand : IRequest<ProcessAcademicRecordResponse>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    public string FapHtmlContent { get; set; } = string.Empty;

    // MODIFIED: The command now accepts the program ID.
    // The handler will be responsible for resolving the latest active version.
    public Guid CurriculumProgramId { get; set; }
}