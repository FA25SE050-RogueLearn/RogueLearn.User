using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class MatchPlayerSummaryRepository : GenericRepository<MatchPlayerSummary>, IMatchPlayerSummaryRepository
{
    public MatchPlayerSummaryRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<List<MatchPlayerSummary>> GetByMatchResultIdAsync(Guid matchResultId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<MatchPlayerSummary>()
            .Where(x => x.MatchResultId == matchResultId)
            .Get(cancellationToken);
        return response.Models;
    }

    public async Task<List<MatchPlayerSummary>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<MatchPlayerSummary>()
            .Where(x => x.SessionId == sessionId)
            .Get(cancellationToken);
        return response.Models;
    }

    public async Task DeleteByMatchResultIdAsync(Guid matchResultId, CancellationToken cancellationToken = default)
    {
        await _supabaseClient
            .From<MatchPlayerSummary>()
            .Filter("match_result_id", Operator.Equals, matchResultId.ToString())
            .Delete(cancellationToken: cancellationToken);
    }

    public async Task<MatchPlayerSummary?> GetLatestByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<MatchPlayerSummary>()
            .Where(x => x.UserId == userId)
            .Order("created_at", Ordering.Descending)
            .Limit(1)
            .Single(cancellationToken);
        return response;
    }

    public async Task<List<Guid>> GetRecentMatchResultIdsByUserAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<MatchPlayerSummary>()
            .Where(x => x.UserId == userId)
            .Order("created_at", Ordering.Descending)
            .Limit(limit)
            .Get(cancellationToken);

        return response.Models
            .Where(m => m.MatchResultId != Guid.Empty)
            .Select(m => m.MatchResultId)
            .Distinct()
            .Take(limit)
            .ToList();
    }
}
