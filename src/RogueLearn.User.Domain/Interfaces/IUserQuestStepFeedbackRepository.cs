using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserQuestStepFeedbackRepository : IGenericRepository<UserQuestStepFeedback>
{
    Task<IEnumerable<UserQuestStepFeedback>> GetByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserQuestStepFeedback>> GetBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserQuestStepFeedback>> GetUnresolvedAsync(CancellationToken cancellationToken = default);
}