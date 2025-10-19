// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetAllRoutes/GetAllRoutesQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;

public class GetAllRoutesQueryHandler : IRequestHandler<GetAllRoutesQuery, List<RouteDto>>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;

    public GetAllRoutesQueryHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
    }

    public async Task<List<RouteDto>> Handle(GetAllRoutesQuery request, CancellationToken cancellationToken)
    {
        var programs = await _curriculumProgramRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<RouteDto>>(programs);
    }
}