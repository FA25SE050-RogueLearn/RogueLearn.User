// src/RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordCommand : IRequest<ProcessAcademicRecordResponse>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    public string FapHtmlContent { get; set; } = string.Empty;

    // We need to know which curriculum this record belongs to,
    // as the HTML might not contain this context.
    public Guid CurriculumVersionId { get; set; }
}