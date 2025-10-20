using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Classes.DTOs;

public record ClassDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? RoadmapUrl,
    string[]? SkillFocusAreas,
    int DifficultyLevel,
    int? EstimatedDurationMonths,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ClassDetailDto FromEntity(Class c) => new(
        c.Id,
        c.Name,
        c.Description,
        c.RoadmapUrl,
        c.SkillFocusAreas,
        c.DifficultyLevel,
        c.EstimatedDurationMonths,
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt
    );
}