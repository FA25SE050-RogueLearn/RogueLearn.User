using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;

/// <summary>
/// Handles retrieval of all onboarding routes (curriculum programs).
/// Adds structured logging and ensures null-safe mapping.
/// </summary>
public class GetAllRoutesQueryHandler : IRequestHandler<GetAllRoutesQuery, List<RouteDto>>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllRoutesQueryHandler> _logger;

    public GetAllRoutesQueryHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper, ILogger<GetAllRoutesQueryHandler> logger)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all curriculum programs and maps them to onboarding routes.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of route DTOs.</returns>
    public async Task<List<RouteDto>> Handle(GetAllRoutesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Handler} - retrieving all onboarding routes", nameof(GetAllRoutesQueryHandler));

        var programs = await _curriculumProgramRepository.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<List<RouteDto>>(programs) ?? new List<RouteDto>();

        _logger.LogInformation("{Handler} - returning {Count} routes", nameof(GetAllRoutesQueryHandler), dtos.Count);

        return dtos;
    }
}