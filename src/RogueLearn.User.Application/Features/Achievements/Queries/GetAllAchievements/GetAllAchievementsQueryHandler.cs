using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;

public class GetAllAchievementsQueryHandler : IRequestHandler<GetAllAchievementsQuery, GetAllAchievementsResponse>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IMapper _mapper;

    public GetAllAchievementsQueryHandler(IAchievementRepository achievementRepository, IMapper mapper)
    {
        _achievementRepository = achievementRepository;
        _mapper = mapper;
    }

    public async Task<GetAllAchievementsResponse> Handle(GetAllAchievementsQuery request, CancellationToken cancellationToken)
    {
        var achievements = await _achievementRepository.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<List<AchievementDto>>(achievements);

        return new GetAllAchievementsResponse
        {
            Achievements = dtos
        };
    }
}