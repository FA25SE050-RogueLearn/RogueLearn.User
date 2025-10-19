using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Classes.Commands.CreateClass;

public record CreateClassCommand(
    string Name,
    string? Description,
    string? RoadmapUrl,
    string[]? SkillFocusAreas,
    int? DifficultyLevel,
    int? EstimatedDurationMonths,
    bool? IsActive
) : IRequest<Class>;
