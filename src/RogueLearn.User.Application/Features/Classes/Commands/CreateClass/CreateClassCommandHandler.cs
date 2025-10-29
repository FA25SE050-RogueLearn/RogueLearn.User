using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Classes.Commands.CreateClass;

public class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, Class>
{
    private readonly IClassRepository _classRepository;

    public CreateClassCommandHandler(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<Class> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Name is required.");

        var exists = await _classRepository.AnyAsync(c => c.Name == request.Name, cancellationToken);
        if (exists)
            throw new BadRequestException($"Class with name '{request.Name}' already exists.");

        var entity = new Class
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            RoadmapUrl = request.RoadmapUrl,
            SkillFocusAreas = request.SkillFocusAreas,
            // Convert incoming int? to domain enum; default to Beginner (1)
            DifficultyLevel = (DifficultyLevel)(request.DifficultyLevel ?? 1),
            EstimatedDurationMonths = request.EstimatedDurationMonths,
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        entity = await _classRepository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
