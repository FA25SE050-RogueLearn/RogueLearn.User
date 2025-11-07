using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using System.IO;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQueryHandler : IRequestHandler<SuggestNoteTagsFromUploadQuery, SuggestNoteTagsResponse>
{
    private readonly ITaggingSuggestionService _suggestionService;

    public SuggestNoteTagsFromUploadQueryHandler(ITaggingSuggestionService suggestionService)
    {
        _suggestionService = suggestionService;
    }

    public async Task<SuggestNoteTagsResponse> Handle(SuggestNoteTagsFromUploadQuery request, CancellationToken cancellationToken)
    {
        if (request.FileStream is null || (request.FileLength ?? 0) <= 0)
        {
            throw new ValidationException("No file content provided.");
        }

        var attachment = new AiFileAttachment
        {
            Stream = request.FileStream,
            ProvidedLength = request.FileLength,
            ContentType = request.ContentType ?? "application/octet-stream",
            FileName = request.FileName ?? string.Empty
        };

        var suggestions = await _suggestionService.SuggestFromFileAsync(request.AuthUserId, attachment, request.MaxTags, cancellationToken);
        return new SuggestNoteTagsResponse { Suggestions = suggestions };
    }
}