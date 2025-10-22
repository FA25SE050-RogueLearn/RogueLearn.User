// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetAllClasses/GetAllClassesQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

/// <summary>
/// Handles retrieval of all active classes for onboarding flows.
/// Adds structured logging and ensures null-safe mapping.
/// </summary>
public class GetAllClassesQueryHandler : IRequestHandler<GetAllClassesQuery, List<ClassDto>>
{
    private readonly IClassRepository _classRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllClassesQueryHandler> _logger;

    public GetAllClassesQueryHandler(IClassRepository classRepository, IMapper mapper, ILogger<GetAllClassesQueryHandler> logger)
    {
        _classRepository = classRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all active classes and maps them to DTOs.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active class DTOs.</returns>
    public async Task<List<ClassDto>> Handle(GetAllClassesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Handler} - retrieving all active classes", nameof(GetAllClassesQueryHandler));

        // FIXED: The Supabase LINQ provider cannot parse an implicit boolean predicate (c => c.IsActive).
        // It requires an explicit comparison (c => c.IsActive == true) to correctly translate it
        // into the required URL filter parameter (is_active=eq.true).
        var classes = await _classRepository.FindAsync(c => c.IsActive == true, cancellationToken);
        var dtos = _mapper.Map<List<ClassDto>>(classes) ?? new List<ClassDto>();

        _logger.LogInformation("{Handler} - returning {Count} active classes", nameof(GetAllClassesQueryHandler), dtos.Count);

        return dtos;
    }
}