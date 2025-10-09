using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.RemoveSubjectFromCurriculum;

public class RemoveSubjectFromCurriculumHandler : IRequestHandler<RemoveSubjectFromCurriculumCommand>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;

    public RemoveSubjectFromCurriculumHandler(ICurriculumStructureRepository curriculumStructureRepository)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
    }

    public async Task Handle(RemoveSubjectFromCurriculumCommand request, CancellationToken cancellationToken)
    {
        var curriculumStructure = await _curriculumStructureRepository.GetByIdAsync(request.Id, cancellationToken);
        if (curriculumStructure == null)
        {
            throw new NotFoundException("CurriculumStructure", request.Id);
        }

        await _curriculumStructureRepository.DeleteAsync(request.Id, cancellationToken);
    }
}