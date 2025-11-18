// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserQuestStepProgressRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserQuestStepProgressRepository : GenericRepository<UserQuestStepProgress>, IUserQuestStepProgressRepository
{
    public UserQuestStepProgressRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    // ADDED: Implementation of the new specialized method.
    /// <summary>
    /// Counts the number of completed steps for a specific quest attempt.
    /// This method uses explicit string-based filtering to avoid enum serialization issues
    /// present in the generic LINQ provider.
    /// </summary>
    /// <param name="attemptId">The ID of the user's quest attempt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The count of completed steps.</returns>
    public async Task<int> GetCompletedStepsCountForAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserQuestStepProgress>()
            .Filter("attempt_id", Operator.Equals, attemptId.ToString())
            // CRITICAL FIX: We explicitly convert the enum to its string representation ("Completed").
            // This ensures the correct value is sent to the database, preventing the "invalid input value for enum" error.
            .Filter("status", Operator.Equals, StepCompletionStatus.Completed.ToString())
            .Count(CountType.Exact, cancellationToken);

        return (int)response;
    }
}