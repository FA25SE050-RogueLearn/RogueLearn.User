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

    public async Task<Quest?> GetActiveQuestBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<Quest>()
            .Filter("subject_id", Operator.Equals, subjectId.ToString())
            .Filter("is_active", Operator.Equals, "true")
            .Single(cancellationToken);

        return response;
    }
}