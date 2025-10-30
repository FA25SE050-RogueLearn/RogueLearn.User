// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Commands/ImportSubjectFromText/ImportSubjectFromTextCommand.cs
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommand : IRequest<CreateSubjectResponse>
{
    public string RawText { get; set; } = string.Empty;
}