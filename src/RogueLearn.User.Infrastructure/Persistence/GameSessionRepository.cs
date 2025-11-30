using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GameSessionRepository : GenericRepository<GameSession>, IGameSessionRepository
{
    public GameSessionRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<GameSession?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GameSession>()
            .Where(x => x.SessionId == sessionId)
            .Single(cancellationToken);
        return response;
    }

    public async Task<GameSession?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GameSession>()
            .Where(x => x.RelayJoinCode == joinCode)
            .Single(cancellationToken);
        return response;
    }

    public async Task<List<GameSession>> GetRecentSessionsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GameSession>()
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(limit)
            .Get(cancellationToken);
        return response.Models;
    }

    public async Task<List<GameSession>> GetRecentSessionsByUserAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GameSession>()
            .Where(x => x.UserId == userId)
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(limit)
            .Get(cancellationToken);
        return response.Models;
    }

    public async Task<GameSession?> GetByMatchResultIdAsync(Guid matchResultId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GameSession>()
            .Where(x => x.MatchResultId == matchResultId)
            .Single(cancellationToken);
        return response;
    }
}
