using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillById;

/// <summary>
/// Handles retrieval of a Skill by Id.
/// - Throws standardized NotFoundException when missing.
/// - Emits structured logs for start and completion.
/// </summary>
public sealed class GetSkillByIdQueryHandler : IRequestHandler<GetSkillByIdQuery, GetSkillByIdResponse>
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<GetSkillByIdQueryHandler> _logger;

    public GetSkillByIdQueryHandler(ISkillRepository skillRepository, ILogger<GetSkillByIdQueryHandler> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a skill by Id.
    /// </summary>
    public async Task<GetSkillByIdResponse> Handle(GetSkillByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetSkillByIdQuery for SkillId={SkillId}", request.Id);

        var skill = await _skillRepository.GetByIdAsync(request.Id, cancellationToken);
        if (skill is null)
        {
            _logger.LogWarning("Skill not found: SkillId={SkillId}", request.Id);
            throw new NotFoundException("Skill", request.Id);
        }

        _logger.LogInformation("Found skill: SkillId={SkillId}, Name={Name}", skill.Id, skill.Name);
        return new GetSkillByIdResponse
        {
            Id = skill.Id,
            Name = skill.Name,
            Domain = skill.Domain,
            // Domain uses SkillTierLevel enum; API DTO expects int
            Tier = (int)skill.Tier,
            Description = skill.Description
        };
    }
}