// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserQuestStepFeedbackRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserQuestStepFeedbackRepository : GenericRepository<UserQuestStepFeedback>, IUserQuestStepFeedbackRepository
{
    public UserQuestStepFeedbackRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<UserQuestStepFeedback>> GetByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserQuestStepFeedback>()
            .Filter("quest_id", Operator.Equals, questId.ToString())
            .Order("created_at", Ordering.Descending)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<UserQuestStepFeedback>> GetBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        // This query aggregates feedback from ALL users who are taking this subject.
        // It allows admins to spot content errors (like broken links) that affect everyone.
        var response = await _supabaseClient
            .From<UserQuestStepFeedback>()
            .Filter("subject_id", Operator.Equals, subjectId.ToString())
            .Order("created_at", Ordering.Descending)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<UserQuestStepFeedback>> GetUnresolvedAsync(CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserQuestStepFeedback>()
            .Filter("is_resolved", Operator.Equals, "false")
            .Order("created_at", Ordering.Descending)
            .Get(cancellationToken);

        return response.Models;
    }
}