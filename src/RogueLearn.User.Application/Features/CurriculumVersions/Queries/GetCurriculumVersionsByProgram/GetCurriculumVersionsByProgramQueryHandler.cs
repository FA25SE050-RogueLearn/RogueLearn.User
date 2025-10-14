using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionsByProgram;

public class GetCurriculumVersionsByProgramQueryHandler : IRequestHandler<GetCurriculumVersionsByProgramQuery, List<CurriculumVersionDto>>
{
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly IMapper _mapper;

    public GetCurriculumVersionsByProgramQueryHandler(ICurriculumVersionRepository curriculumVersionRepository, IMapper mapper)
    {
        _curriculumVersionRepository = curriculumVersionRepository;
        _mapper = mapper;
    }

    public async Task<List<CurriculumVersionDto>> Handle(GetCurriculumVersionsByProgramQuery request, CancellationToken cancellationToken)
    {
        var versions = await _curriculumVersionRepository.FindAsync(v => v.ProgramId == request.ProgramId, cancellationToken);
        return _mapper.Map<List<CurriculumVersionDto>>(versions.ToList());
    }
}