using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using BuildingBlocks.Shared.Extensions; // ToSlug

namespace RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;

public class CommitNoteTagSelectionsCommandHandler : IRequestHandler<CommitNoteTagSelectionsCommand, CommitNoteTagSelectionsResponse>
{
    private readonly INoteRepository _noteRepository;
    private readonly ITagRepository _tagRepository;
    private readonly INoteTagRepository _noteTagRepository;

    public CommitNoteTagSelectionsCommandHandler(
        INoteRepository noteRepository,
        ITagRepository tagRepository,
        INoteTagRepository noteTagRepository)
    {
        _noteRepository = noteRepository;
        _tagRepository = tagRepository;
        _noteTagRepository = noteTagRepository;
    }

    public async Task<CommitNoteTagSelectionsResponse> Handle(CommitNoteTagSelectionsCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
        if (note is null || note.AuthUserId != request.AuthUserId)
        {
            throw new ValidationException("Note not found or access denied.");
        }

        // Retrieve user's existing tags once and construct a valid ID set
        var userTags = (await _tagRepository.FindAsync(t => t.AuthUserId == request.AuthUserId, cancellationToken)).ToList();
        var validTagIds = userTags.Select(t => t.Id).ToHashSet();

        // Filter incoming selected IDs to those that actually exist for this user
        var selectedValid = request.SelectedTagIds
            .Where(id => validTagIds.Contains(id))
            .Distinct()
            .ToList();

        // Create new tags if needed, ensuring uniqueness by slug for this user
        var createdTags = new List<CreatedTagDto>();
        foreach (var raw in request.NewTagNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var name = raw.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var slug = name.ToSlug();
            // Attempt slug match in memory from user's tags
            var tag = userTags.FirstOrDefault(t => t.Name.ToSlug() == slug);
            if (tag is null)
            {
                tag = new Tag
                {
                    AuthUserId = request.AuthUserId,
                    Name = name
                };
                await _tagRepository.AddAsync(tag, cancellationToken);
                createdTags.Add(new CreatedTagDto { Id = tag.Id, Name = tag.Name });

                // Track newly created tag for subsequent selection & validation
                userTags.Add(tag);
                validTagIds.Add(tag.Id);
            }
            // Add to selected list if not already present
            if (!selectedValid.Contains(tag.Id))
                selectedValid.Add(tag.Id);
        }

        // Re-fetch persisted tags to guard against any transient insert issues or RLS constraints
        var persistedTagIds = (await _tagRepository.FindAsync(t => t.AuthUserId == request.AuthUserId, cancellationToken))
            .Select(t => t.Id)
            .ToHashSet();
        selectedValid = selectedValid.Where(id => persistedTagIds.Contains(id)).Distinct().ToList();

        // Get current tag ids for note
        var currentTagIds = await _noteTagRepository.GetTagIdsForNoteAsync(note.Id, cancellationToken);
        var desired = selectedValid.Distinct().ToHashSet();

        // Add missing
        foreach (var tagId in desired.Except(currentTagIds))
        {
            await _noteTagRepository.AddAsync(note.Id, tagId, cancellationToken);
        }
        // Remove extras
        foreach (var tagId in currentTagIds.Except(desired))
        {
            await _noteTagRepository.RemoveAsync(note.Id, tagId, cancellationToken);
        }

        return new CommitNoteTagSelectionsResponse
        {
            NoteId = note.Id,
            AddedTagIds = desired.ToList(),
            CreatedTags = createdTags,
            TotalTagsAssigned = desired.Count
        };
    }
}