using MediatR;
using RogueLearn.User.Application.Models;
using System.IO;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQuery : IRequest<SuggestNoteTagsResponse>
{
    public Guid AuthUserId { get; set; }
    // Stream-based file attribute to avoid large byte[] copies in application layer
    public Stream? FileStream { get; set; }
    public long? FileLength { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int MaxTags { get; set; } = 10;
}