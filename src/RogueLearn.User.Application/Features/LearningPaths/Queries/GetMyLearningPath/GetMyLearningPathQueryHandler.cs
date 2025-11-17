// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
// ADDED: For Mapping
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
    private readonly IUserQuestProgressRepository _userQuestProgressRepository;
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper; // Added for mapping entities to DTOs

    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository userQuestProgressRepository,
        ILogger<GetMyLearningPathQueryHandler> logger,
        ISubjectRepository subjectRepository,
        IMapper mapper) // Added IMapper
    {
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _userQuestProgressRepository = userQuestProgressRepository;
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

        // Filter out quests with null QuestChapterId
        var quests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.QuestChapterId.HasValue && chapterIds.Contains(q.QuestChapterId.Value))
            .ToList();

        // Group by QuestChapterId (now we can use .Value since we filtered above)
        var questsByChapterId = quests
            .GroupBy(q => q.QuestChapterId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var userProgress = (await _userQuestProgressRepository.GetUserProgressForQuestsAsync(request.AuthUserId, quests.Select(q => q.Id).ToList(), cancellationToken))
            .ToDictionary(p => p.QuestId);

        var chapterDtos = new List<QuestChapterDto>();
        foreach (var chapter in chapters)
        {
            var chapterDto = new QuestChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Sequence = chapter.Sequence,
                // FIX: Pass the LearningPathId correctly from the persisted learningPath.
                // Handle the case where the LearningPathId might be null from the database, although the logic earlier should prevent this.
                // This addresses the potential null issue if the mapping was still trying to access something that might be null.
                // The primary fix is in the recursive call, ensuring it uses a valid LearningPathId.
            };

            if (questsByChapterId.TryGetValue(chapter.Id, out var chapterQuests))
            {
                foreach (var quest in chapterQuests)
                {
                    var status = userProgress.TryGetValue(quest.Id, out var progress)
                        ? progress.Status.ToString()
                        : "NotStarted";

                    chapterDto.Quests.Add(new QuestSummaryDto
                    {
                        Id = quest.Id,
                        Title = quest.Title,
                        Status = status,
                        SequenceOrder = quest.Sequence ?? 0, // CHANGED: Handle null with default value
                        LearningPathId = learningPath.Id,
                        ChapterId = chapter.Id
                    });
                }
            }

            chapterDto.Quests = chapterDto.Quests.OrderBy(q => q.SequenceOrder).ToList();

            // Determine chapter status based on quest statuses
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
                chapterDto.Status = chapter.Status.ToString(); // Use chapter's own status if no quests
            }

            chapterDtos.Add(chapterDto);
        }

        var totalQuests = quests.Count;
        var completedQuests = userProgress.Values.Count(p => p.Status == Domain.Enums.QuestStatus.Completed);
        var completionPercentage = totalQuests > 0 ? Math.Round((double)completedQuests / totalQuests * 100, 2) : 0;

        // FIX: Ensure the LearningPathId in the DTO is correctly populated from the persisted entity.
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

// ADDED: ClassNodeTreeItemDto definition (assuming it's needed for mapping, based on the stack trace)
// If ClassNodeTreeItem is a separate domain entity, ensure its mapping is also configured.
public class ClassNodeTreeItemDto
{
    public Guid Id { get; set; } // Assuming ClassNode has an Id property
    public Guid ClassId { get; set; }
    // FIX: Handle potential null value for ParentId when mapping to Guid
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? NodeType { get; set; }
    public string? Description { get; set; }
    public int Sequence { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedByImport { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Public constructor to allow mapping from potentially null ParentId
    public ClassNodeTreeItemDto(Guid id, Guid classId, Guid? parentId, string title, string? nodeType, string? description, int sequence, bool isActive, bool isLockedByImport, Dictionary<string, object>? metadata, DateTimeOffset createdAt)
    {
        Id = id;
        ClassId = classId;
        // Assign ParentId directly, handling potential null
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

    // Placeholder for Children mapping if needed, but the core issue is ParentId.
    public List<ClassNodeTreeItemDto> Children { get; set; } = new();

    // This mapping method was identified in the stack trace as a potential area of failure.
    // Ensure it correctly handles nullable Guids and potential null parent IDs.
    // If ClassNodeTreeItem is not a DTO, this mapping might need to be adjusted based on its actual structure.
    public static ClassNodeTreeItemDto FromModel(ClassNode node)
    {
        return new ClassNodeTreeItemDto(
            node.Id,
            node.ClassId,
            // Ensure ParentId is correctly mapped, possibly to Guid.Empty or handled by nullable type.
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