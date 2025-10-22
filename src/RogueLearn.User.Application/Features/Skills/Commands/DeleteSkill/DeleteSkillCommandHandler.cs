using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;

/// <summary>
/// Handles deletion of a Skill.
/// - Checks existence and throws standardized NotFoundException when missing.
/// - Emits structured logs for start and completion.
/// </summary>
public sealed class DeleteSkillCommandHandler : IRequestHandler<DeleteSkillCommand>
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<DeleteSkillCommandHandler> _logger;

    public DeleteSkillCommandHandler(ISkillRepository skillRepository, ILogger<DeleteSkillCommandHandler> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    /// <summary>
    /// Deletes a skill by Id.
    /// </summary>
    public async Task Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling DeleteSkillCommand for SkillId={SkillId}", request.Id);

        var exists = await _skillRepository.ExistsAsync(request.Id, cancellationToken);
        if (!exists)
        {
            _logger.LogWarning("Skill not found: SkillId={SkillId}", request.Id);
            throw new NotFoundException("Skill", request.Id);
        }

        await _skillRepository.DeleteAsync(request.Id, cancellationToken);
        _logger.LogInformation("Deleted skill: SkillId={SkillId}", request.Id);
    }
}