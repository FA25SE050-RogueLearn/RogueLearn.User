using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;

public class GetAllCurriculumProgramsQueryHandler : IRequestHandler<GetAllCurriculumProgramsQuery, List<CurriculumProgramDto>>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;

    public GetAllCurriculumProgramsQueryHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
    }

    public async Task<List<CurriculumProgramDto>> Handle(GetAllCurriculumProgramsQuery request, CancellationToken cancellationToken)
    {
        var programs = await _curriculumProgramRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<CurriculumProgramDto>>(programs);
    }
}