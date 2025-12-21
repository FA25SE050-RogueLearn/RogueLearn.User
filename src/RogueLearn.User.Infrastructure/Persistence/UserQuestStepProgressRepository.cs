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

    public async Task<int> GetCompletedStepsCountForAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserQuestStepProgress>()
            .Filter("attempt_id", Operator.Equals, attemptId.ToString())
            .Filter("status", Operator.Equals, StepCompletionStatus.Completed.ToString())
            .Count(CountType.Exact, cancellationToken);

        return (int)response;
    }

    public async Task<IEnumerable<UserQuestStepProgress>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserQuestStepProgress>()
            .Filter("attempt_id", Operator.Equals, attemptId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task DeleteByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        await _supabaseClient
            .From<UserQuestStepProgress>()
            .Filter("attempt_id", Operator.Equals, attemptId.ToString())
            .Delete(cancellationToken: cancellationToken);
    }
}