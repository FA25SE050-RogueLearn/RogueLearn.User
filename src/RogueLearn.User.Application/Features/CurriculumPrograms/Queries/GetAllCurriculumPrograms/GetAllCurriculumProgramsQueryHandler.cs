using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;

/// <summary>
/// Handles retrieval of all curriculum programs.
/// Adds structured logging and ensures null-safe mapping.
/// </summary>
public class GetAllCurriculumProgramsQueryHandler : IRequestHandler<GetAllCurriculumProgramsQuery, List<CurriculumProgramDto>>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllCurriculumProgramsQueryHandler> _logger;

    public GetAllCurriculumProgramsQueryHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper, ILogger<GetAllCurriculumProgramsQueryHandler> logger)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all curriculum programs and maps them to DTOs.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of curriculum program DTOs.</returns>
    public async Task<List<CurriculumProgramDto>> Handle(GetAllCurriculumProgramsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Handler} - retrieving all curriculum programs", nameof(GetAllCurriculumProgramsQueryHandler));

        var programs = await _curriculumProgramRepository.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<List<CurriculumProgramDto>>(programs) ?? new List<CurriculumProgramDto>();

        _logger.LogInformation("{Handler} - returning {Count} curriculum programs", nameof(GetAllCurriculumProgramsQueryHandler), dtos.Count);

        return dtos;
    }
}