// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IUserQuestStepFeedbackRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserQuestStepFeedbackRepository : IGenericRepository<UserQuestStepFeedback>
{
    /// <summary>
    /// Gets feedback for a specific quest instance (User view).
    /// </summary>
    Task<IEnumerable<UserQuestStepFeedback>> GetByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated feedback for a master subject (Admin view).
    /// This allows identifying content errors in the syllabus that affect multiple users.
    /// </summary>
    Task<IEnumerable<UserQuestStepFeedback>> GetBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unresolved feedback items for admin triage.
    /// </summary>
    Task<IEnumerable<UserQuestStepFeedback>> GetUnresolvedAsync(CancellationToken cancellationToken = default);
}