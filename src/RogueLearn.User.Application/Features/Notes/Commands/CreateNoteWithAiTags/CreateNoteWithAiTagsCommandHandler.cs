using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Plugins;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;

/// <summary>
/// Handler for creating a note with AI-generated tag suggestions.
/// - Validates incoming content (raw text or file).
/// - Persists the note and optionally applies suggested tags via mediator.
/// - Emits structured logs for observability and troubleshooting.
/// </summary>
public class CreateNoteWithAiTagsCommandHandler : IRequestHandler<CreateNoteWithAiTagsCommand, CreateNoteWithAiTagsResponse>
{
    private readonly INoteRepository _noteRepository;
    private readonly ITaggingSuggestionService _suggestionService;
    private readonly ISummarizationPlugin _summarizationPlugin;
    private readonly IFileSummarizationPlugin _fileSummarizationPlugin;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateNoteWithAiTagsCommandHandler> _logger;

    public CreateNoteWithAiTagsCommandHandler(
        INoteRepository noteRepository,
        ITaggingSuggestionService suggestionService,
        ISummarizationPlugin summarizationPlugin,
        IFileSummarizationPlugin fileSummarizationPlugin,
        IMediator mediator,
        ILogger<CreateNoteWithAiTagsCommandHandler> logger)
    {
        _noteRepository = noteRepository;
        _suggestionService = suggestionService;
        _summarizationPlugin = summarizationPlugin;
        _fileSummarizationPlugin = fileSummarizationPlugin;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="CreateNoteWithAiTagsCommand"/> by validating input, creating a note, and optionally applying AI tag suggestions.
    /// </summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response containing the created note details and tag suggestions.</returns>
    /// <exception cref="ValidationException">Thrown when input content is missing or cannot be extracted.</exception>
    public async Task<CreateNoteWithAiTagsResponse> Handle(CreateNoteWithAiTagsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreateNoteWithAiTagsCommand for AuthUserId={AuthUserId}, TitleProvided={HasTitle}, ApplySuggestions={ApplySuggestions}, MaxTags={MaxTags}",
            request.AuthUserId, !string.IsNullOrWhiteSpace(request.Title), request.ApplySuggestions, request.MaxTags);

        // Validate presence of content
        var hasRaw = !string.IsNullOrWhiteSpace(request.RawText);
        var hasFile = request.FileStream is not null && (request.FileLength ?? 0) > 0;
        if (!hasRaw && !hasFile)
        {
            _logger.LogWarning("No content provided for note creation (raw text or file).");
            throw new ValidationException("Either raw text or a file upload must be provided.");
        }

        // Resolve note content via AI summary. If AI fails to summarize, DO NOT create the note.
        string noteContent;
        if (hasRaw)
        {
            var raw = request.RawText!.Trim();
            noteContent = await _summarizationPlugin.SummarizeTextAsync(raw, cancellationToken);
            if (string.IsNullOrWhiteSpace(noteContent))
            {
                _logger.LogWarning("AI summarization returned empty content for raw text. Aborting note creation.");
                throw new ValidationException("AI summarization failed. The note was not created.");
            }
        }
        else
        {
            var attachment = new AiFileAttachment
            {
                Stream = request.FileStream!,
                ProvidedLength = request.FileLength,
                ContentType = request.ContentType ?? "application/octet-stream",
                FileName = request.FileName ?? string.Empty
            };
            noteContent = await _fileSummarizationPlugin.SummarizeAsync(attachment, cancellationToken);
            if (string.IsNullOrWhiteSpace(noteContent))
            {
                _logger.LogWarning("AI summarization returned empty content for uploaded file. Aborting note creation. FileName={FileName}, ContentType={ContentType}", request.FileName, request.ContentType);
                throw new ValidationException("AI summarization failed. The note was not created.");
            }
        }

        // Determine title
        var title = (request.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            if (!string.IsNullOrWhiteSpace(request.FileName))
            {
                title = Path.GetFileNameWithoutExtension(request.FileName);
            }
            else
            {
                // Use first non-empty line of the final content as a fallback title
                title = noteContent.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "New Note";
                if (title.Length > 120)
                    title = title[..120];
            }
        }

        // Create the note
        var note = new Note
        {
            AuthUserId = request.AuthUserId,
            Title = title,
            Content = noteContent,
            IsPublic = request.IsPublic,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var created = await _noteRepository.AddAsync(note, cancellationToken);
        _logger.LogInformation("Created note: NoteId={NoteId}, Title={Title}, IsPublic={IsPublic}", created.Id, created.Title, created.IsPublic);

        // Generate AI tag suggestions (file-first for uploads)
        IReadOnlyList<TagSuggestionDto> suggestions;
        if (hasRaw)
        {
            suggestions = await _suggestionService.SuggestAsync(request.AuthUserId, request.RawText!.Trim(), request.MaxTags, cancellationToken);
        }
        else
        {
            // Rewind the stream before reusing it for tag suggestions if possible
            if (request.FileStream is not null && request.FileStream.CanSeek)
            {
                request.FileStream.Position = 0;
            }

            var attachment = new AiFileAttachment
            {
                Stream = request.FileStream!,
                ProvidedLength = request.FileLength,
                ContentType = request.ContentType ?? "application/octet-stream",
                FileName = request.FileName ?? string.Empty
            };
            suggestions = await _suggestionService.SuggestFromFileAsync(request.AuthUserId, attachment, request.MaxTags, cancellationToken);
        }
        _logger.LogInformation("Generated {Count} AI tag suggestions for NoteId={NoteId}", suggestions.Count, created.Id);

        IReadOnlyList<Guid> appliedTagIds = Array.Empty<Guid>();
        IReadOnlyList<CreatedTagDto> createdTags = Array.Empty<CreatedTagDto>();
        int totalAssigned = 0;

        if (request.ApplySuggestions && suggestions.Count > 0)
        {
            var selectedIds = suggestions.Where(s => s.MatchedTagId.HasValue).Select(s => s.MatchedTagId!.Value).Distinct().ToList();
            var newNames = suggestions.Where(s => !s.MatchedTagId.HasValue).Select(s => s.Label.Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var commit = new CommitNoteTagSelectionsCommand
            {
                AuthUserId = request.AuthUserId,
                NoteId = created.Id,
                SelectedTagIds = selectedIds,
                NewTagNames = newNames
            };
            var commitResult = await _mediator.Send(commit, cancellationToken);

            appliedTagIds = commitResult.AddedTagIds;
            createdTags = commitResult.CreatedTags;
            totalAssigned = commitResult.TotalTagsAssigned;
            _logger.LogInformation("Applied suggestions for NoteId={NoteId}: Assigned={Assigned}, CreatedTags={CreatedCount}", created.Id, totalAssigned, createdTags.Count);
        }

        return new CreateNoteWithAiTagsResponse
        {
            NoteId = created.Id,
            Title = created.Title,
            Suggestions = suggestions,
            AppliedTagIds = appliedTagIds,
            CreatedTags = createdTags,
            TotalTagsAssigned = totalAssigned
        };
    }
}