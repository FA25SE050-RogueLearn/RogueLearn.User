// src/RogueLearn.User.Infrastructure/Persistence/QuestChapterRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;

namespace RogueLearn.User.Infrastructure.Persistence;

public class QuestChapterRepository : GenericRepository<QuestChapter>, IQuestChapterRepository
{
    public QuestChapterRepository(Supabase.Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<QuestChapter>> GetChaptersByLearningPathIdOrderedAsync(Guid learningPathId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<QuestChapter>()
            .Where(qc => qc.LearningPathId == learningPathId)
            .Order("sequence", Constants.Ordering.Ascending)
            .Get(cancellationToken);

        return response.Models;
    }
}