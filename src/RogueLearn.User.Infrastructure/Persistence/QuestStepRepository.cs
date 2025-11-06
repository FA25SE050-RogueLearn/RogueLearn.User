// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/QuestStepRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

/// <summary>
/// Implements the IQuestStepRepository for interacting with the "quest_steps" table
/// in the Supabase PostgreSQL database.
/// </summary>
public class QuestStepRepository : GenericRepository<QuestStep>, IQuestStepRepository
{
    public QuestStepRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    // MODIFICATION START: Implemented the new specialized method.
    // This uses a direct .Filter call which is more reliable for Supabase queries
    // than the generic LINQ expression-based FindAsync.
    public async Task<IEnumerable<QuestStep>> FindByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<QuestStep>()
            .Filter("quest_id", Operator.Equals, questId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
    // MODIFICATION END
}