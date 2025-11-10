// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Commands/ImportSubjectFromText/ImportSubjectFromTextCommand.cs
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommand : IRequest<CreateSubjectResponse>
{
    public string RawText { get; set; } = string.Empty;
    // MODIFIED: The command now takes AuthUserId to derive the full context.
    [JsonIgnore] // This is set by the handler, not the client.
    public Guid AuthUserId { get; set; }
}

// MODIFIED: The request DTO from the client is simplified. It no longer needs ProgramId.
public class ImportSubjectFromTextRequest
{
    public string RawText { get; set; } = string.Empty;
}