// building_blocks/BuildingBlocks.Shared/DTOs/NoteDto.cs
using System.Text.Json.Serialization;

namespace BuildingBlocks.Shared.DTOs;

/// <summary>
/// A Data Transfer Object for sharing note content between services.
/// The UserMicroservice will return this, and the QuestMicroservice will consume it.
/// </summary>
public class NoteDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}