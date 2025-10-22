using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionsByProgram;

public class GetCurriculumVersionsByProgramQueryHandler : IRequestHandler<GetCurriculumVersionsByProgramQuery, List<CurriculumVersionDto>>
{
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCurriculumVersionsByProgramQueryHandler> _logger;

    public GetCurriculumVersionsByProgramQueryHandler(ICurriculumVersionRepository curriculumVersionRepository, IMapper mapper, ILogger<GetCurriculumVersionsByProgramQueryHandler> logger)
    {
        _curriculumVersionRepository = curriculumVersionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves curriculum versions for a given program and maps them to DTOs.
    /// </summary>
    /// <param name="request">The request containing the ProgramId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of CurriculumVersionDto for the specified program.</returns>
    public async Task<List<CurriculumVersionDto>> Handle(GetCurriculumVersionsByProgramQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching curriculum versions for program {ProgramId}", request.ProgramId);
        var versions = await _curriculumVersionRepository.FindAsync(v => v.ProgramId == request.ProgramId, cancellationToken);
        var list = (versions?.ToList()) ?? new List<Domain.Entities.CurriculumVersion>();
        _logger.LogInformation("Found {VersionCount} versions for program {ProgramId}", list.Count, request.ProgramId);
        return _mapper.Map<List<CurriculumVersionDto>>(list);
    }
}