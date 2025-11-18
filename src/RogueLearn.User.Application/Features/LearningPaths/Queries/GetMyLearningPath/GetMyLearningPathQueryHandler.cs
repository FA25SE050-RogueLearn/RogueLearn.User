// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandler : IRequestHandler<GetMyLearningPathQuery, LearningPathDto?>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    // MODIFICATION: The UserQuestAttemptRepository is no longer needed for deriving status.
    // private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        // IUserQuestAttemptRepository userQuestAttemptRepository, // REMOVED
        ILogger<GetMyLearningPathQueryHandler> logger,
        ISubjectRepository subjectRepository,
        IMapper mapper)
    {
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        // _userQuestAttemptRepository = userQuestAttemptRepository; // REMOVED
        _logger = logger;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<LearningPathDto?> Handle(GetMyLearningPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching primary learning path for user {AuthUserId}", request.AuthUserId);

        var learningPath = (await _learningPathRepository.FindAsync(lp => lp.CreatedBy == request.AuthUserId, cancellationToken))
            .OrderByDescending(lp => lp.CreatedAt)
            .FirstOrDefault();

        if (learningPath == null)
        {
            _logger.LogWarning("No learning path found for user {AuthUserId}", request.AuthUserId);
            return null;
        }

        var chapters = (await _questChapterRepository.FindAsync(qc => qc.LearningPathId == learningPath.Id, cancellationToken))
            .OrderBy(c => c.Sequence)
            .ToList();

        var chapterIds = chapters.Select(c => c.Id).ToList();

        var quests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.QuestChapterId.HasValue && chapterIds.Contains(q.QuestChapterId.Value))
            .ToList();

        var questsByChapterId = quests
            .GroupBy(q => q.QuestChapterId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // MODIFICATION: This dictionary is no longer needed as status is read directly from the quest.
        // var userAttempts = (await _userQuestAttemptRepository.FindAsync(a => a.AuthUserId == request.AuthUserId && quests.Select(q => q.Id).Contains(a.QuestId), cancellationToken))
        //     .GroupBy(a => a.QuestId)
        //     .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).FirstOrDefault());

        var chapterDtos = new List<QuestChapterDto>();
        foreach (var chapter in chapters)
        {
            var chapterDto = new QuestChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Sequence = chapter.Sequence,
            };

            if (questsByChapterId.TryGetValue(chapter.Id, out var chapterQuests))
            {
                foreach (var quest in chapterQuests)
                {
                    // MODIFICATION: The status is now read directly from the quest entity itself.
                    // This simplifies the query and relies on the new single source of truth.
                    var status = quest.Status.ToString();

                    chapterDto.Quests.Add(new QuestSummaryDto
                    {
                        Id = quest.Id,
                        Title = quest.Title,
                        Status = status,
                        SequenceOrder = quest.Sequence ?? 0,
                        LearningPathId = learningPath.Id,
                        ChapterId = chapter.Id
                    });
                }
            }

            chapterDto.Quests = chapterDto.Quests.OrderBy(q => q.SequenceOrder).ToList();

            if (chapterDto.Quests.Any())
            {
                var allCompleted = chapterDto.Quests.All(q => q.Status == QuestStatus.Completed.ToString());
                var anyInProgress = chapterDto.Quests.Any(q => q.Status == QuestStatus.InProgress.ToString());
                var anyCompleted = chapterDto.Quests.Any(q => q.Status == QuestStatus.Completed.ToString());

                if (allCompleted) chapterDto.Status = PathProgressStatus.Completed.ToString();
                else if (anyInProgress || anyCompleted) chapterDto.Status = PathProgressStatus.InProgress.ToString();
                else chapterDto.Status = PathProgressStatus.NotStarted.ToString();
            }
            else
            {
                chapterDto.Status = chapter.Status.ToString();
            }

            chapterDtos.Add(chapterDto);
        }

        // MODIFICATION: Calculate completion percentage dynamically since the summary table is gone.
        var totalQuests = quests.Count;
        var completedQuests = quests.Count(q => q.Status == QuestStatus.Completed);
        var completionPercentage = totalQuests > 0 ? Math.Round((double)completedQuests / totalQuests * 100, 2) : 0;

        var learningPathDto = new LearningPathDto
        {
            Id = learningPath.Id,
            Name = learningPath.Name,
            Description = learningPath.Description,
            Chapters = chapterDtos,
            CompletionPercentage = completionPercentage
        };

        return learningPathDto;
    }
}

public class ClassNodeTreeItemDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? NodeType { get; set; }
    public string? Description { get; set; }
    public int Sequence { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedByImport { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ClassNodeTreeItemDto(Guid id, Guid classId, Guid? parentId, string title, string? nodeType, string? description, int sequence, bool isActive, bool isLockedByImport, Dictionary<string, object>? metadata, DateTimeOffset createdAt)
    {
        Id = id;
        ClassId = classId;
        ParentId = parentId;
        Title = title;
        NodeType = nodeType;
        Description = description;
        Sequence = sequence;
        IsActive = isActive;
        IsLockedByImport = isLockedByImport;
        Metadata = metadata;
        CreatedAt = createdAt;
    }

    public List<ClassNodeTreeItemDto> Children { get; set; } = new();

    public static ClassNodeTreeItemDto FromModel(ClassNode node)
    {
        return new ClassNodeTreeItemDto(
            node.Id,
            node.ClassId,
            node.ParentId,
            node.Title,
            node.NodeType,
            node.Description,
            node.Sequence,
            node.IsActive,
            node.IsLockedByImport,
            node.Metadata,
            node.CreatedAt
        );
    }
}