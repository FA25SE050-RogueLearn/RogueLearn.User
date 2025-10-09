using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Commands.ActivateCurriculumVersion;

public class ActivateCurriculumVersionCommandHandler : IRequestHandler<ActivateCurriculumVersionCommand>
{
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ICurriculumVersionActivationRepository _activationRepository;

    public ActivateCurriculumVersionCommandHandler(
        ICurriculumVersionRepository curriculumVersionRepository,
        ICurriculumVersionActivationRepository activationRepository)
    {
        _curriculumVersionRepository = curriculumVersionRepository;
        _activationRepository = activationRepository;
    }

    public async Task Handle(ActivateCurriculumVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _curriculumVersionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (version == null)
        {
            throw new NotFoundException("CurriculumVersion", request.CurriculumVersionId);
        }

        version.IsActive = true; // ensure it’s active when activation is recorded
        await _curriculumVersionRepository.UpdateAsync(version, cancellationToken);

        var activation = new CurriculumVersionActivation
        {
            CurriculumVersionId = request.CurriculumVersionId,
            EffectiveYear = request.EffectiveYear,
            ActivatedBy = request.ActivatedBy,
            Notes = request.Notes
        };

        await _activationRepository.AddAsync(activation, cancellationToken);
    }
}