using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;

/// <summary>
/// Handles creation of a new Skill.
/// - Validates uniqueness by name.
/// - Emits structured logs for traceability.
/// - Returns a typed response DTO.
/// </summary>
public sealed class CreateSkillCommandHandler : IRequestHandler<CreateSkillCommand, CreateSkillResponse>
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<CreateSkillCommandHandler> _logger;

    public CreateSkillCommandHandler(ISkillRepository skillRepository, ILogger<CreateSkillCommandHandler> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a skill after enforcing uniqueness by name.
    /// </summary>
    public async Task<CreateSkillResponse> Handle(CreateSkillCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreateSkillCommand for Name={Name}", request.Name);

        // Enforce uniqueness on Name
        var existsWithName = await _skillRepository.AnyAsync(s => s.Name == request.Name, cancellationToken);
        if (existsWithName)
        {
            _logger.LogWarning("Skill with Name={Name} already exists", request.Name);
            throw new ConflictException($"Skill '{request.Name}' already exists.");
        }

        var skill = new Skill
        {
            Name = request.Name,
            Domain = request.Domain,
            Tier = request.Tier,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var created = await _skillRepository.AddAsync(skill, cancellationToken);
        _logger.LogInformation("Created skill: SkillId={SkillId}, Name={Name}", created.Id, created.Name);

        return new CreateSkillResponse
        {
            Id = created.Id,
            Name = created.Name,
            Domain = created.Domain,
            Tier = created.Tier,
            Description = created.Description
        };
    }
}