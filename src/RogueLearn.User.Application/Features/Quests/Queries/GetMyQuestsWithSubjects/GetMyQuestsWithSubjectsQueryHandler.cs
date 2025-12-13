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

        var attempts = (await _attemptRepository.FindAsync(a => a.AuthUserId == request.AuthUserId, cancellationToken)).ToList();
        if (!attempts.Any())
        {
            _logger.LogInformation("No quest attempts found for user {AuthUserId}", request.AuthUserId);
            return new List<MyQuestWithSubjectDto>();
        }

        var questIds = attempts.Select(a => a.QuestId).Distinct().ToList();
        var quests = (await _questRepository.GetByIdsAsync(questIds, cancellationToken))
            .Where(q => q.IsActive)
            .ToList();

        if (!quests.Any())
        {
            _logger.LogInformation("No active quests found for attempts of user {AuthUserId}", request.AuthUserId);
            return new List<MyQuestWithSubjectDto>();
        }

        var subjectIds = quests
            .Where(q => q.SubjectId.HasValue)
            .Select(q => q.SubjectId!.Value)
            .Distinct()
            .ToList();

        var subjects = await _subjectRepository.GetByIdsAsync(subjectIds, cancellationToken);
        var subjectMap = subjects.ToDictionary(s => s.Id, s => s);

        var result = quests.Select(q =>
        {
            subjectMap.TryGetValue(q.SubjectId ?? Guid.Empty, out var subj);
            return new MyQuestWithSubjectDto
            {
                QuestId = q.Id,
                Title = q.Title,
                Status = q.Status.ToString(),
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

