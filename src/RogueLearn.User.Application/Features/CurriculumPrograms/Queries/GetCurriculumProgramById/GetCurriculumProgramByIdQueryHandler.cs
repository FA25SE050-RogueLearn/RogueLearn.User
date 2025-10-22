using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramById;

/// <summary>
/// Handles retrieval of a curriculum program by its ID.
/// Adds structured logging and preserves NotFound behavior when the program is missing.
/// </summary>
public class GetCurriculumProgramByIdQueryHandler : IRequestHandler<GetCurriculumProgramByIdQuery, CurriculumProgramDto>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCurriculumProgramByIdQueryHandler> _logger;

    public GetCurriculumProgramByIdQueryHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper, ILogger<GetCurriculumProgramByIdQueryHandler> logger)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a curriculum program by id and maps it to a DTO.
    /// </summary>
    /// <param name="request">The query request containing the program id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The curriculum program DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the program cannot be found.</exception>
    public async Task<CurriculumProgramDto> Handle(GetCurriculumProgramByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Handler} - retrieving curriculum program by Id={ProgramId}", nameof(GetCurriculumProgramByIdQueryHandler), request.Id);

        var program = await _curriculumProgramRepository.GetByIdAsync(request.Id, cancellationToken);
        if (program == null)
        {
            _logger.LogWarning("{Handler} - curriculum program not found for Id={ProgramId}", nameof(GetCurriculumProgramByIdQueryHandler), request.Id);
            throw new NotFoundException("CurriculumProgram", request.Id);
        }

        var dto = _mapper.Map<CurriculumProgramDto>(program);
        _logger.LogInformation("{Handler} - returning curriculum program for Id={ProgramId}", nameof(GetCurriculumProgramByIdQueryHandler), request.Id);
        return dto;
    }
}