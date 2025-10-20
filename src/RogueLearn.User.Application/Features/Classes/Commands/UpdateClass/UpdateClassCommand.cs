using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Classes.Commands.UpdateClass;

public record UpdateClassCommand(
    Guid Id,
    string? Name,
    string? Description,
    string? RoadmapUrl,
    string[]? SkillFocusAreas,
    int? DifficultyLevel,
    int? EstimatedDurationMonths,
    bool? IsActive
) : IRequest<Class>;
