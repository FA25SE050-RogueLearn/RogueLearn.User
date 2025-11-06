// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestProgress/Queries/GetUserProgressForQuest/GetUserProgressForQuestQueryHandler.cs
using MediatR;
using RogueLearn.User.Domain.Interfaces;
// MODIFICATION: Namespace updated.
namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetUserProgressForQuest;

// MODIFICATION: Class name and request/response types updated.
public class GetUserProgressForQuestQueryHandler : IRequestHandler<GetUserProgressForQuestQuery, GetUserProgressForQuestResponse?>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;

    public GetUserProgressForQuestQueryHandler(IUserQuestAttemptRepository attemptRepository, IUserQuestStepProgressRepository stepProgressRepository)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
    }

    public async Task<GetUserProgressForQuestResponse?> Handle(GetUserProgressForQuestQuery request, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        // MODIFICATION START: Instead of returning null when no progress is found,
        // we now construct a default response with a "NotStarted" status. This ensures
        // the controller returns a 200 OK instead of a 404 Not Found.
        if (attempt == null)
        {
            // No progress has been recorded for this quest, so return a default "Not Started" state.
            return new GetUserProgressForQuestResponse
            {
                QuestId = request.QuestId,
                QuestStatus = "NotStarted", // Explicitly set the status.
                StepStatuses = new Dictionary<Guid, string>() // Return an empty dictionary for steps.
            };
        }
        // MODIFICATION END

        var stepProgresses = await _stepProgressRepository.FindAsync(sp => sp.AttemptId == attempt.Id, cancellationToken);

        return new GetUserProgressForQuestResponse
        {
            QuestId = request.QuestId,
            QuestStatus = attempt.Status.ToString(),
            StepStatuses = stepProgresses.ToDictionary(sp => sp.StepId, sp => sp.Status.ToString())
        };
    }
}