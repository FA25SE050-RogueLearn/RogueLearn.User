using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;

public class SuggestNoteTagsQueryHandler : IRequestHandler<SuggestNoteTagsQuery, SuggestNoteTagsResponse>
{
    private readonly ITaggingSuggestionService _suggestionService;
    private readonly INoteRepository _noteRepository;

    public SuggestNoteTagsQueryHandler(ITaggingSuggestionService suggestionService, INoteRepository noteRepository)
    {
        _suggestionService = suggestionService;
        _noteRepository = noteRepository;
    }

    public async Task<SuggestNoteTagsResponse> Handle(SuggestNoteTagsQuery request, CancellationToken cancellationToken)
    {
        // Validate: either RawText or NoteId must be provided
        if (string.IsNullOrWhiteSpace(request.RawText) && !request.NoteId.HasValue)
        {
            throw new ValidationException("Either rawText or noteId must be provided.");
        }

        var raw = request.RawText;
        if (string.IsNullOrWhiteSpace(raw) && request.NoteId.HasValue)
        {
            var note = await _noteRepository.GetByIdAsync(request.NoteId.Value, cancellationToken);
            if (note is null || note.AuthUserId != request.AuthUserId)
            {
                throw new ValidationException("Note not found or access denied.");
            }
            raw = ExtractRawText(note.Content);
        }

        var suggestions = await _suggestionService.SuggestAsync(request.AuthUserId, raw!, request.MaxTags, cancellationToken);
        return new SuggestNoteTagsResponse { Suggestions = suggestions };
    }

    private static string? ExtractRawText(object? content)
    {
        if (content is null) return null;

        if (content is string s)
        {
            // Try to parse as JSON string (e.g., "text") and return the inner value
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(s, (JsonSerializerOptions?)null);
                if (element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString();
                }
                // If it's a JSON object/array, fall back to the raw string
                return s;
            }
            catch
            {
                // Not JSON -> treat as plain text
                return s;
            }
        }

        if (content is JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String) return el.GetString();
            // For non-string JSON, return the compact JSON text
            return el.ToString();
        }

        // Fallback: serialize structured content to JSON text
        return JsonSerializer.Serialize(content, (JsonSerializerOptions?)null);
    }
}