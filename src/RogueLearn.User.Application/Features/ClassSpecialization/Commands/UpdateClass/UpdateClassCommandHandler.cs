using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.UpdateClass;

public class UpdateClassCommandHandler : IRequestHandler<UpdateClassCommand, ClassDto>
{
    private readonly IClassRepository _classRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateClassCommandHandler> _logger;

    public UpdateClassCommandHandler(
        IClassRepository classRepository,
        IMapper mapper,
        ILogger<UpdateClassCommandHandler> logger)
    {
        _classRepository = classRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ClassDto> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await _classRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
        {
            throw new NotFoundException("Class", request.Id);
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.RoadmapUrl = request.RoadmapUrl;
        entity.SkillFocusAreas = request.SkillFocusAreas?.ToArray();
        entity.DifficultyLevel = request.DifficultyLevel;
        entity.EstimatedDurationMonths = request.EstimatedDurationMonths;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await _classRepository.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated Class {ClassId}", updated.Id);

        return _mapper.Map<ClassDto>(updated);
    }
}