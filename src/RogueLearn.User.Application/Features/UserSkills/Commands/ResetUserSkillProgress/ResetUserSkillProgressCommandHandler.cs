using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.ResetUserSkillProgress;

public sealed class ResetUserSkillProgressCommandHandler : IRequestHandler<ResetUserSkillProgressCommand>
{
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly IValidator<ResetUserSkillProgressCommand> _validator;
    private readonly ILogger<ResetUserSkillProgressCommandHandler> _logger;

    public ResetUserSkillProgressCommandHandler(
        IUserSkillRepository userSkillRepository,
        IValidator<ResetUserSkillProgressCommand> validator,
        ILogger<ResetUserSkillProgressCommandHandler> logger)
    {
        _userSkillRepository = userSkillRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task Handle(ResetUserSkillProgressCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var skill = await _userSkillRepository.FirstOrDefaultAsync(
            s => s.AuthUserId == request.AuthUserId && s.SkillName == request.SkillName,
            cancellationToken);

        if (skill is null)
        {
            throw new NotFoundException("UserSkill", request.SkillName);
        }

        var before = new { skill.ExperiencePoints, skill.Level, skill.LastUpdatedAt };

        skill.ExperiencePoints = 0;
        skill.Level = 1;
        skill.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _userSkillRepository.UpdateAsync(skill, cancellationToken);

        _logger.LogInformation(
            "Admin reset user skill progression for AuthUserId {AuthUserId}, Skill '{SkillName}'. Reason: {Reason}. Before: {@Before}, After: {@After}",
            request.AuthUserId,
            request.SkillName,
            request.Reason,
            before,
            new { skill.ExperiencePoints, skill.Level, skill.LastUpdatedAt });
    }
}