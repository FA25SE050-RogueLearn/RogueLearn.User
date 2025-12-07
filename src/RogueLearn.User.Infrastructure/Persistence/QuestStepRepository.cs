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

    public async Task<IEnumerable<QuestStep>> GetByQuestIdAsync(Guid questId,
        CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<QuestStep>()
            .Filter("quest_id", Operator.Equals, questId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<bool> QuestContainsSteps(Guid questId, CancellationToken cancellationToken = default)
    {
        var steps = await _supabaseClient
            .From<QuestStep>()
            // --- MODIFICATION START: Convert the Guid to a string ---
            // The Supabase client's Filter method requires a primitive type like a string for comparison.
            .Filter("quest_id", Operator.Equals, questId.ToString())
            // --- MODIFICATION END ---
            .Count(CountType.Exact);

        return steps > 0;
    }
}