using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;

public class GetMyQuestsWithSubjectsQueryHandler : IRequestHandler<GetMyQuestsWithSubjectsQuery, List<MyQuestWithSubjectDto>>
{
    private readonly IQuestRepository _questRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GetMyQuestsWithSubjectsQueryHandler> _logger;

    public GetMyQuestsWithSubjectsQueryHandler(
        IQuestRepository questRepository,
        ISubjectRepository subjectRepository,
        ILogger<GetMyQuestsWithSubjectsQueryHandler> logger)
    {
        _questRepository = questRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<List<MyQuestWithSubjectDto>> Handle(GetMyQuestsWithSubjectsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching quests for user {AuthUserId}", request.AuthUserId);

        var allQuests = await _questRepository.GetAllAsync(cancellationToken);
        var userQuests = allQuests
            .Where(q => q.CreatedBy == request.AuthUserId && q.IsActive)
            .ToList();

        var subjectIds = userQuests
            .Where(q => q.SubjectId.HasValue)
            .Select(q => q.SubjectId!.Value)
            .Distinct()
            .ToList();

        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var subjectMap = subjects
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionary(s => s.Id, s => s);

        var result = userQuests.Select(q =>
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

