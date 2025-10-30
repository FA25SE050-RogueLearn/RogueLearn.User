using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserQuestProgressRepository : GenericRepository<UserQuestProgress>, IUserQuestProgressRepository
{
    public UserQuestProgressRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    // NEW METHOD IMPLEMENTATION:
    /// <summary>
    /// Efficiently fetches quest progress records for a specific user filtered by a list of quest IDs.
    /// </summary>
    /// <param name="authUserId">The user's authentication ID.</param>
    /// <param name="questIds">The list of quest IDs to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of matching UserQuestProgress records.</returns>
    public async Task<IEnumerable<UserQuestProgress>> GetUserProgressForQuestsAsync(Guid authUserId, List<Guid> questIds, CancellationToken cancellationToken = default)
    {
        // This is the architecturally correct way to perform this query.
        // It uses the Supabase client's native filtering capabilities, which will generate an efficient
        // `in` clause in the PostgREST URL (e.g., `quest_id=in.(guid1,guid2)`).
        if (questIds == null || !questIds.Any())
        {
            return Enumerable.Empty<UserQuestProgress>();
        }

        var response = await _supabaseClient
            .From<UserQuestProgress>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("quest_id", Operator.In, questIds.Select(id => id.ToString()).ToList())
            .Get(cancellationToken);

        return response.Models;
    }
}
