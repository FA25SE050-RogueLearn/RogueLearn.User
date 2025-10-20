using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;

public sealed class DeleteSkillCommandHandler : IRequestHandler<DeleteSkillCommand>
{
    private readonly ISkillRepository _skillRepository;

    public DeleteSkillCommandHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
    {
        var exists = await _skillRepository.ExistsAsync(request.Id, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Skill not found");
        }
        await _skillRepository.DeleteAsync(request.Id, cancellationToken);
    }
}