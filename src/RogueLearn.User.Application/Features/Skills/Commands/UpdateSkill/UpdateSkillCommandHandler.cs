using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;

public sealed class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, UpdateSkillResponse>
{
    private readonly ISkillRepository _skillRepository;

    public UpdateSkillCommandHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<UpdateSkillResponse> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
    {
        var existing = await _skillRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("Skill not found");

        existing.Name = request.Name;
        existing.Domain = request.Domain;
        existing.Tier = request.Tier;
        existing.Description = request.Description;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await _skillRepository.UpdateAsync(existing, cancellationToken);

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