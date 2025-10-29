using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.UpdateCurriculumStructure;

public class UpdateCurriculumStructureHandler : IRequestHandler<UpdateCurriculumStructureCommand, UpdateCurriculumStructureResponse>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly IMapper _mapper;

    public UpdateCurriculumStructureHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        IMapper mapper)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _mapper = mapper;
    }

    public async Task<UpdateCurriculumStructureResponse> Handle(UpdateCurriculumStructureCommand request, CancellationToken cancellationToken)
    {
        var curriculumStructure = await _curriculumStructureRepository.GetByIdAsync(request.Id, cancellationToken);
        if (curriculumStructure == null)
        {
            throw new NotFoundException("CurriculumStructure", request.Id);
        }

        curriculumStructure.Semester = request.TermNumber;
        curriculumStructure.IsMandatory = request.IsMandatory;
        curriculumStructure.PrerequisiteSubjectIds = request.PrerequisiteSubjectIds;
        curriculumStructure.PrerequisitesText = request.PrerequisitesText;

        var updated = await _curriculumStructureRepository.UpdateAsync(curriculumStructure, cancellationToken);
        return _mapper.Map<UpdateCurriculumStructureResponse>(updated);
    }
}