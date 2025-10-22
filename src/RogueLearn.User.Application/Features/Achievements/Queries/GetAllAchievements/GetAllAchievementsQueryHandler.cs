using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;

/// <summary>
/// Handles retrieval of all achievements in the system.
/// Adds structured logging and ensures a null-safe mapping to the response DTOs.
/// </summary>
public class GetAllAchievementsQueryHandler : IRequestHandler<GetAllAchievementsQuery, GetAllAchievementsResponse>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllAchievementsQueryHandler> _logger;

    public GetAllAchievementsQueryHandler(
        IAchievementRepository achievementRepository,
        IMapper mapper,
        ILogger<GetAllAchievementsQueryHandler> logger)
    {
        _achievementRepository = achievementRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all achievements and maps them to DTOs.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response containing the list of all achievements.</returns>
    public async Task<GetAllAchievementsResponse> Handle(GetAllAchievementsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Handler} - retrieving all achievements", nameof(GetAllAchievementsQueryHandler));

        var achievements = await _achievementRepository.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<List<AchievementDto>>(achievements) ?? new List<AchievementDto>();

        _logger.LogInformation("{Handler} - retrieved {Count} achievements", nameof(GetAllAchievementsQueryHandler), dtos.Count);

        return new GetAllAchievementsResponse
        {
            Achievements = dtos
        };
    }
}