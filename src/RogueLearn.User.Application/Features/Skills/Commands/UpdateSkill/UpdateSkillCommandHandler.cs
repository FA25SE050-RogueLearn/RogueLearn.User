using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;

/// <summary>
/// Handles updating an existing Skill.
/// - Loads skill and throws standardized NotFoundException when missing.
/// - Emits structured logs for start and completion.
/// - Returns a typed response DTO.
/// </summary>
public sealed class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, UpdateSkillResponse>
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<UpdateSkillCommandHandler> _logger;

    public UpdateSkillCommandHandler(ISkillRepository skillRepository, ILogger<UpdateSkillCommandHandler> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    /// <summary>
    /// Updates skill properties and persists changes.
    /// </summary>
    public async Task<UpdateSkillResponse> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UpdateSkillCommand for SkillId={SkillId}", request.Id);

        var existing = await _skillRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Skill not found: SkillId={SkillId}", request.Id);
            throw new NotFoundException("Skill", request.Id);
        }

        existing.Name = request.Name;
        existing.Domain = request.Domain;
        existing.Tier = request.Tier;
        existing.Description = request.Description;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await _skillRepository.UpdateAsync(existing, cancellationToken);
        _logger.LogInformation("Updated skill: SkillId={SkillId}, Name={Name}", updated.Id, updated.Name);

        return new UpdateSkillResponse
        {
            Id = updated.Id,
            Name = updated.Name,
            Domain = updated.Domain,
            Tier = updated.Tier,
            Description = updated.Description
        };
    }
}