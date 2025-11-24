// src/RogueLearn.User.Infrastructure/Persistence/LearningPathRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;

namespace RogueLearn.User.Infrastructure.Persistence;

public class LearningPathRepository : GenericRepository<LearningPath>, ILearningPathRepository
{
    public LearningPathRepository(Supabase.Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<LearningPath?> GetLatestByUserAsync(Guid createdBy, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<LearningPath>()
            .Where(lp => lp.CreatedBy == createdBy)
            .Order("created_at", Constants.Ordering.Descending)
            .Limit(1)
            .Get(cancellationToken);

        return response.Models.FirstOrDefault();
    }
}