using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNoteFromUpload;

public class CreateNoteFromUploadCommandHandler : IRequestHandler<CreateNoteFromUploadCommand, CreateNoteResponse>
{
    private readonly INoteRepository _noteRepository;
    private readonly IMapper _mapper;
    private readonly IFileSummarizationPlugin _fileSummarizationPlugin;
    private readonly ILogger<CreateNoteFromUploadCommandHandler> _logger;

    public CreateNoteFromUploadCommandHandler(INoteRepository noteRepository, IMapper mapper, IFileSummarizationPlugin fileSummarizationPlugin, ILogger<CreateNoteFromUploadCommandHandler> logger)
    {
        _noteRepository = noteRepository;
        _mapper = mapper;
        _fileSummarizationPlugin = fileSummarizationPlugin;
        _logger = logger;
    }

    public async Task<CreateNoteResponse> Handle(CreateNoteFromUploadCommand request, CancellationToken cancellationToken)
    {
        // Prefer AI summarization to produce a structured BlockNote document object.
        object? contentObject = null;
        try
        {
            var attachment = new AiFileAttachment
            {
                Stream = request.FileStream,
                ContentType = request.ContentType,
                FileName = request.FileName
            };
            contentObject = await _fileSummarizationPlugin.SummarizeAsync(attachment, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI summarization failed during upload-and-create. Falling back to plain text conversion. FileName={FileName}", request.FileName);
        }

        if (contentObject is null)
        {
            // Fallback: read plaintext and convert to BlockNote
            string extractedContent;
            if (request.FileStream.CanSeek)
            {
                try { request.FileStream.Position = 0; } catch { /* ignore */ }
            }
            using var reader = new StreamReader(request.FileStream);
            extractedContent = await reader.ReadToEndAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(extractedContent))
                throw new BadRequestException("Could not extract any content from the provided file.");

            contentObject = BlockNoteDocumentFactory.FromPlainText(extractedContent);
        }

        var note = new Note
        {
            AuthUserId = request.AuthUserId,
            Title = Path.GetFileNameWithoutExtension(request.FileName),
            // Store structured BlockNote document as JSONB
            Content = contentObject,
            IsPublic = false // User-uploaded content is private by default
        };

        // Reduced logging for insert diagnostics
        var createdNote = await _noteRepository.AddAsync(note, cancellationToken);
        _logger.LogInformation("Created note from upload. Id={NoteId}, Title={Title}", createdNote.Id, createdNote.Title);

        return _mapper.Map<CreateNoteResponse>(createdNote);
    }
}