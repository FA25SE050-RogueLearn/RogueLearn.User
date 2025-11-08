namespace RogueLearn.User.Application.Features.Meetings.DTOs;

public class ArtifactInputDto
{
    public string ArtifactType { get; set; } = string.Empty; // e.g., recording, transcript, notes
    public string Url { get; set; } = string.Empty;
}