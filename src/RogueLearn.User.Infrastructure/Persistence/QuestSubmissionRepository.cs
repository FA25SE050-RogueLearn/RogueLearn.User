// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/QuestSubmissionRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using System.Linq.Expressions;

namespace RogueLearn.User.Infrastructure.Persistence;

public class QuestSubmissionRepository : GenericRepository<QuestSubmission>, IQuestSubmissionRepository
{
    public QuestSubmissionRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<List<QuestSubmission>> GetByActivityIdAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        var predicate = (Expression<Func<QuestSubmission, bool>>)(x => x.ActivityId == activityId);
        var result = await FindAsync(predicate, cancellationToken);
        return result.ToList();
    }

    public async Task<QuestSubmission?> GetLatestByActivityAndUserAsync(Guid activityId, Guid userId, CancellationToken cancellationToken = default)
    {
        var predicate = (Expression<Func<QuestSubmission, bool>>)(x => x.ActivityId == activityId && x.UserId == userId);
        var submissions = await FindAsync(predicate, cancellationToken);
        return submissions.OrderByDescending(x => x.SubmittedAt).FirstOrDefault();
    }
}
