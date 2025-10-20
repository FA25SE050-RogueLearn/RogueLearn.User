using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQuery : IRequest<SuggestNoteTagsResponse>
{
    public Guid AuthUserId { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int MaxTags { get; set; } = 10;
}