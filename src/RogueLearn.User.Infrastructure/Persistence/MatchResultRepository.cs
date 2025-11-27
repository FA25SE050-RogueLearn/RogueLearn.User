using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class MatchResultRepository : GenericRepository<MatchResult>, IMatchResultRepository
{

    public MatchResultRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<MatchResult?> GetByMatchIdAsync(string matchId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<MatchResult>()
            .Where(x => x.MatchId == matchId)
            .Single(cancellationToken);
        return response;
    }

    public async Task<List<MatchResult>> GetRecentMatchesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<MatchResult>()
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(limit)
            .Get(cancellationToken);
        return response.Models;
    }

    public async Task<List<MatchResult>> GetMatchesByUserAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<MatchResult>()
            .Where(x => x.UserId == userId)
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(limit)
            .Get(cancellationToken);
        return response.Models;
    }
}
