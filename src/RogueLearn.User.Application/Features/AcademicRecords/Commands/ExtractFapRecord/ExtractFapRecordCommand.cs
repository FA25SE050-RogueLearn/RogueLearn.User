// RogueLearn.User/src/RogueLearn.User.Application/Features/AcademicRecords/Commands/ExtractFapRecord/ExtractFapRecordCommand.cs
using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.AcademicRecords.Commands.ExtractFapRecord;

// This class implements IRequest<FapRecordData>, ensuring that the MediatR Send method
// will return a Task<FapRecordData>, which resolves the "cannot assign void" error.
public class ExtractFapRecordCommand : IRequest<FapRecordData>
{
    public string FapHtmlContent { get; set; } = string.Empty;
}