// RogueLearn.User/src/RogueLearn.User.Application/Features/Notes/Commands/CreateNoteFromUpload/CreateNoteFromUploadCommandHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNoteFromUpload;

public class CreateNoteFromUploadCommandHandler : IRequestHandler<CreateNoteFromUploadCommand, CreateNoteResponse>
{
    private readonly INoteRepository _noteRepository;
    private readonly IMapper _mapper;

    public CreateNoteFromUploadCommandHandler(INoteRepository noteRepository, IMapper mapper)
    {
        _noteRepository = noteRepository;
        _mapper = mapper;
    }

    public async Task<CreateNoteResponse> Handle(CreateNoteFromUploadCommand request, CancellationToken cancellationToken)
    {
        // In a real application, you would use a library like iTextSharp (for PDF)
        // or Open-XML-SDK (for DOCX) to extract text here.
        // For this example, we'll simulate text extraction.
        string extractedContent;
        using (var reader = new StreamReader(request.FileStream))
        {
            extractedContent = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(extractedContent))
        {
            throw new BadRequestException("Could not extract any content from the provided file.");
        }

        var note = new Note
        {
            AuthUserId = request.AuthUserId,
            Title = Path.GetFileNameWithoutExtension(request.FileName),
            Content = extractedContent, // Storing extracted text directly
            IsPublic = false // User-uploaded content is private by default
        };

        var createdNote = await _noteRepository.AddAsync(note, cancellationToken);

        return _mapper.Map<CreateNoteResponse>(createdNote);
    }
}