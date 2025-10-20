using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.RemoveUserSkill;

public sealed class RemoveUserSkillCommandHandler : IRequestHandler<RemoveUserSkillCommand>
{
    private readonly IUserSkillRepository _userSkillRepository;

    public RemoveUserSkillCommandHandler(IUserSkillRepository userSkillRepository)
    {
        _userSkillRepository = userSkillRepository;
    }

    public async Task Handle(RemoveUserSkillCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userSkillRepository.FirstOrDefaultAsync(
            s => s.AuthUserId == request.AuthUserId && s.SkillName == request.SkillName,
            cancellationToken);

        if (existing is not null)
        {
            await _userSkillRepository.DeleteAsync(existing.Id, cancellationToken);
        }
        // Idempotent: if not found, do nothing
    }
}