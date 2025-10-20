using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQueryHandler : IRequestHandler<SuggestNoteTagsFromUploadQuery, SuggestNoteTagsResponse>
{
    private readonly IFileTextExtractor _fileTextExtractor;
    private readonly ITaggingSuggestionService _suggestionService;

    public SuggestNoteTagsFromUploadQueryHandler(IFileTextExtractor fileTextExtractor, ITaggingSuggestionService suggestionService)
    {
        _fileTextExtractor = fileTextExtractor;
        _suggestionService = suggestionService;
    }

    public async Task<SuggestNoteTagsResponse> Handle(SuggestNoteTagsFromUploadQuery request, CancellationToken cancellationToken)
    {
        if (request.FileContent is null || request.FileContent.Length == 0)
        {
            throw new ValidationException("No file content provided.");
        }

        using var stream = new MemoryStream(request.FileContent, writable: false);
        var text = await _fileTextExtractor.ExtractTextAsync(stream, request.ContentType ?? string.Empty, request.FileName ?? string.Empty, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException("Unable to extract text from file.");
        }

        var suggestions = await _suggestionService.SuggestAsync(request.AuthUserId, text, request.MaxTags, cancellationToken);
        return new SuggestNoteTagsResponse { Suggestions = suggestions };
    }
}