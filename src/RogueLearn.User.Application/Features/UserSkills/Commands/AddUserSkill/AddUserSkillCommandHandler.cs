using FluentValidation;
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.AddUserSkill;

public sealed class AddUserSkillCommandHandler : IRequestHandler<AddUserSkillCommand, AddUserSkillResponse>
{
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly IValidator<AddUserSkillCommand> _validator;

    public AddUserSkillCommandHandler(IUserSkillRepository userSkillRepository, IValidator<AddUserSkillCommand> validator)
    {
        _userSkillRepository = userSkillRepository;
        _validator = validator;
    }

    public async Task<AddUserSkillResponse> Handle(AddUserSkillCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var existing = await _userSkillRepository.FirstOrDefaultAsync(
            s => s.AuthUserId == request.AuthUserId && s.SkillName == request.SkillName,
            cancellationToken);

        UserSkill userSkill;
        if (existing is null)
        {
            userSkill = new UserSkill
            {
                Id = Guid.NewGuid(),
                AuthUserId = request.AuthUserId,
                SkillName = request.SkillName.Trim(),
                ExperiencePoints = 0,
                Level = 1,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            userSkill = await _userSkillRepository.AddAsync(userSkill, cancellationToken);
        }
        else
        {
            userSkill = existing;
        }

        return new AddUserSkillResponse
        {
            Id = userSkill.Id,
            AuthUserId = userSkill.AuthUserId,
            SkillName = userSkill.SkillName,
            ExperiencePoints = userSkill.ExperiencePoints,
            Level = userSkill.Level,
            LastUpdatedAt = userSkill.LastUpdatedAt
        };
    }
}