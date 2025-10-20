using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;

public sealed class CreateSkillCommandHandler : IRequestHandler<CreateSkillCommand, CreateSkillResponse>
{
    private readonly ISkillRepository _skillRepository;

    public CreateSkillCommandHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<CreateSkillResponse> Handle(CreateSkillCommand request, CancellationToken cancellationToken)
    {
        // Optional uniqueness check on Name
        var existsWithName = await _skillRepository.AnyAsync(s => s.Name == request.Name, cancellationToken);
        if (existsWithName)
        {
            throw new InvalidOperationException($"Skill with name '{request.Name}' already exists.");
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