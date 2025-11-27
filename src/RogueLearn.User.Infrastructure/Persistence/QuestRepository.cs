// src/RogueLearn.User.Infrastructure/Persistence/QuestRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class QuestRepository : GenericRepository<Quest>, IQuestRepository
{
    public QuestRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<Quest>> GetQuestsByChapterIdsAsync(IEnumerable<Guid> chapterIds, CancellationToken cancellationToken = default)
    {
        var chapterIdList = chapterIds.ToList();
        if (!chapterIdList.Any())
            return Enumerable.Empty<Quest>();

        // Use the Filter method with "In" operator to work with Supabase's PostgREST API
        // This translates to: quest_chapter_id=in.(guid1,guid2,...)
        var response = await _supabaseClient
            .From<Quest>()
            .Filter("quest_chapter_id", Operator.In, chapterIdList.Select(id => id.ToString()).ToList())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<Quest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var list = ids.ToList();
        if (!list.Any()) return Enumerable.Empty<Quest>();

        var response = await _supabaseClient
            .From<Quest>()
            .Filter("id", Operator.In, list.Select(id => id.ToString()).ToList())
            .Get(cancellationToken);

        return response.Models;
    }
}