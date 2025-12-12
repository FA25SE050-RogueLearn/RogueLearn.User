using MediatR;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.GameSessions.Queries.GetGameSessionPlayers
{
    public record GetGameSessionPlayersQuery(Guid SessionId) : IRequest<IReadOnlyList<GetGameSessionPlayersResponse>>;

    public record GetGameSessionPlayersResponse(
        Guid Id,
        Guid? UserId,
        long? ClientId,
        int TotalQuestions,
        int CorrectAnswers,
        double AverageTime,
        object? TopicBreakdown,
        DateTimeOffset CreatedAt);

    public class GetGameSessionPlayersQueryHandler : IRequestHandler<GetGameSessionPlayersQuery, IReadOnlyList<GetGameSessionPlayersResponse>>
    {
        private readonly IMatchPlayerSummaryRepository _matchPlayerSummaryRepository;
        private readonly IGameSessionRepository _gameSessionRepository;

        public GetGameSessionPlayersQueryHandler(
            IMatchPlayerSummaryRepository matchPlayerSummaryRepository,
            IGameSessionRepository gameSessionRepository)
        {
            _matchPlayerSummaryRepository = matchPlayerSummaryRepository;
            _gameSessionRepository = gameSessionRepository;
        }

        public async Task<IReadOnlyList<GetGameSessionPlayersResponse>> Handle(GetGameSessionPlayersQuery request, CancellationToken cancellationToken)
        {
            var summaries = await _matchPlayerSummaryRepository.GetBySessionIdAsync(request.SessionId);

            if (summaries.Count == 0)
            {
                var session = await _gameSessionRepository.GetBySessionIdAsync(request.SessionId);
                if (session?.MatchResultId != null)
                {
                    summaries = await _matchPlayerSummaryRepository.GetByMatchResultIdAsync(session.MatchResultId.Value);
                }
            }

            var list = summaries.Select(s =>
            {
                object? topics = null;
                if (!string.IsNullOrWhiteSpace(s.TopicBreakdownJson))
                {
                    try
                    {
                        topics = JsonSerializer.Deserialize<object>(s.TopicBreakdownJson!);
                    }
                    catch
                    {
                        topics = s.TopicBreakdownJson;
                    }
                }

                return new GetGameSessionPlayersResponse(
                    s.Id,
                    s.UserId,
                    s.ClientId,
                    s.TotalQuestions,
                    s.CorrectAnswers,
                    s.AverageTime ?? 0d,
                    topics,
                    s.CreatedAt);
            }).ToList();

            return list;
        }
    }
}
