using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Classes.Commands.UpdateClass;

public class UpdateClassCommandHandler : IRequestHandler<UpdateClassCommand, Class>
{
    private readonly IClassRepository _classRepository;

    public UpdateClassCommandHandler(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<Class> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await _classRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException($"Class {request.Id} not found.");

        if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name!.Trim();
        if (request.Description is not null) entity.Description = request.Description;
        if (request.RoadmapUrl is not null) entity.RoadmapUrl = request.RoadmapUrl;
        if (request.SkillFocusAreas is not null) entity.SkillFocusAreas = request.SkillFocusAreas;
        if (request.DifficultyLevel.HasValue) entity.DifficultyLevel = request.DifficultyLevel.Value;
        if (request.EstimatedDurationMonths.HasValue) entity.EstimatedDurationMonths = request.EstimatedDurationMonths.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity = await _classRepository.UpdateAsync(entity, cancellationToken);
        return entity;
    }
}
