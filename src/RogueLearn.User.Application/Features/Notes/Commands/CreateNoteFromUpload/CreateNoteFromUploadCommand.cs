// RogueLearn.User/src/RogueLearn.User.Application/Features/Notes/Commands/CreateNoteFromUpload/CreateNoteFromUploadCommand.cs
using MediatR;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNoteFromUpload;

public class CreateNoteFromUploadCommand : IRequest<CreateNoteResponse>
{
    public Guid AuthUserId { get; set; }
    public Stream FileStream { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}