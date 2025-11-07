using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;

public class CreateNoteWithAiTagsCommand : IRequest<CreateNoteWithAiTagsResponse>
{
    public Guid AuthUserId { get; set; }
    public string? Title { get; set; }

    // Raw text path
    public string? RawText { get; set; }

    // Upload path (use stream instead of raw bytes)
    public Stream? FileStream { get; set; }
    public long? FileLength { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }

    public bool IsPublic { get; set; } = false;
    public int MaxTags { get; set; } = 10;
    public bool ApplySuggestions { get; set; } = true;
}