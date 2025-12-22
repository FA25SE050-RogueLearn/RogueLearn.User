using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;

public class GetMyQuestsWithSubjectsQueryHandler : IRequestHandler<GetMyQuestsWithSubjectsQuery, List<MyQuestWithSubjectDto>>
{
    private readonly IQuestRepository _questRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly ILogger<GetMyQuestsWithSubjectsQueryHandler> _logger;

    public GetMyQuestsWithSubjectsQueryHandler(
        IQuestRepository questRepository,
        ISubjectRepository subjectRepository,
        IUserQuestAttemptRepository attemptRepository,
        ILogger<GetMyQuestsWithSubjectsQueryHandler> logger)
    {
        _questRepository = questRepository;
        _subjectRepository = subjectRepository;
        _attemptRepository = attemptRepository;
        _logger = logger;
    }

    public async Task<List<MyQuestWithSubjectDto>> Handle(GetMyQuestsWithSubjectsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching quests for user {AuthUserId}", request.AuthUserId);

        // 1. Get the user's specific attempts
        var attempts = (await _attemptRepository.FindAsync(a => a.AuthUserId == request.AuthUserId, cancellationToken)).ToList();
        if (!attempts.Any())
        {
            _logger.LogInformation("No quest attempts found for user {AuthUserId}", request.AuthUserId);
            return new List<MyQuestWithSubjectDto>();
        }

        // 2. Map Attempt to Quest ID for joining later
        var attemptMap = attempts.ToDictionary(a => a.QuestId, a => a);
        var questIds = attemptMap.Keys.ToList();

        // 3. Get the Master Quest definitions
        var quests = (await _questRepository.GetByIdsAsync(questIds, cancellationToken))
            .Where(q => q.IsActive) // Keep IsActive check for soft-deletes
            .ToList();

        if (!quests.Any())
        {
            _logger.LogInformation("No active quests found for attempts of user {AuthUserId}", request.AuthUserId);
            return new List<MyQuestWithSubjectDto>();
        }

        // 4. Get Subjects
        var subjectIds = quests
            .Where(q => q.SubjectId.HasValue)
            .Select(q => q.SubjectId!.Value)
            .Distinct()
            .ToList();

        var subjects = await _subjectRepository.GetByIdsAsync(subjectIds, cancellationToken);
        var subjectMap = subjects.ToDictionary(s => s.Id, s => s);

        // 5. Construct DTO
        // CRITICAL FIX: The 'Status' field must reflect the USER'S progress (from Attempt),
        // not the Master Quest's status (which would just be 'Published').
        var result = quests.Select(q =>
        {
            subjectMap.TryGetValue(q.SubjectId ?? Guid.Empty, out var subj);
            var userAttempt = attemptMap.GetValueOrDefault(q.Id);

            return new MyQuestWithSubjectDto
            {
                QuestId = q.Id,
                Title = q.Title,
                // Map the User's Attempt Status (e.g., InProgress, Completed)
                // Fallback to "NotStarted" if for some reason the attempt is missing (shouldn't happen given logic above)
                Status = userAttempt?.Status.ToString() ?? "NotStarted",
                SubjectId = q.SubjectId,
                SubjectCode = subj?.SubjectCode,
                SubjectName = subj?.SubjectName,
                Credits = subj?.Credits
            };
        }).ToList();

        _logger.LogInformation("Returning {Count} quests for user {AuthUserId}", result.Count, request.AuthUserId);
        return result;
    }
}