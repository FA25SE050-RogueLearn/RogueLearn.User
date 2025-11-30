// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestLine/GenerateQuestLineCommandHandler.cs
// ⭐ FIXED: Use class_specialization_subjects.semester for class subjects

using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

public class GenerateQuestLineCommandHandler : IRequestHandler<GenerateQuestLine, GenerateQuestLineResponse>
{
    private static readonly HashSet<string> ExcludedSubjectCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "VOV114",
        "VOV124",
        "VOV134",  // Vovinam
        "TMI101",  // Musical instruments
        "OTP101",  // Orientation
        "TRS601",  // English 6
        "PEN"      // if you actually use this code; adjust as needed
    };

    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;
    private readonly IStudentSemesterSubjectRepository _studentSemesterSubjectRepository;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISubjectRepository subjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IStudentSemesterSubjectRepository studentSemesterSubjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        ILogger<GenerateQuestLineCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _subjectRepository = subjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _studentSemesterSubjectRepository = studentSemesterSubjectRepository;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task<GenerateQuestLineResponse> Handle(GenerateQuestLine request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId);
        if (userProfile is null)
        {
            throw new BadRequestException("Invalid User Profile");
        }

        if (userProfile.RouteId is null)
        {
            throw new BadRequestException("Please select a route first.");
        }

        if (userProfile.ClassId is null)
        {
            throw new BadRequestException("Please select a class first.");
        }

        _logger.LogInformation("Reconciling QuestLine structure for User: {username}", userProfile.Username);

        // ARCHITECTURAL CHANGE: Fetch existing learning path or create a new one. DO NOT DELETE.
        var learningPath = (await _learningPathRepository.FindAsync(lp => lp.CreatedBy == request.AuthUserId, cancellationToken))
            .FirstOrDefault();

        if (learningPath == null)
        {
            _logger.LogInformation("No existing learning path found. Creating a new one for user {AuthUserId}", request.AuthUserId);
            learningPath = new LearningPath
            {
                Name = $"{userProfile.Username}'s Path of Knowledge",
                Description = $"Your learning journey awaits!",
                PathType = PathType.Course,
                IsPublished = true,
                CreatedBy = userProfile.AuthUserId
            };
            learningPath = await _learningPathRepository.AddAsync(learningPath, cancellationToken);
        }

        // --- Start Reconciliation Logic ---

        // ⭐ FIX: Phase 1 - Get curriculum program subjects
        var routeSubjects = (await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken)).ToList();
        _logger.LogInformation("Retrieved {Count} subjects from curriculum program", routeSubjects.Count);

        // ⭐ FIX: Phase 2 - Get class specialization subjects WITH their semester values
        var classSpecializationRecords = (await _classSpecializationSubjectRepository.FindAsync(
            css => css.ClassId == userProfile.ClassId.Value, cancellationToken)).ToList();

        _logger.LogInformation("Found {Count} class specialization records", classSpecializationRecords.Count);

        // ⭐ IMPORTANT: Extract subject IDs and create a mapping of SubjectId -> Semester
        var classSpecializationSubjectIds = classSpecializationRecords
            .Select(css => css.SubjectId)
            .ToHashSet();

        // Create a dictionary: SubjectId -> Semester (from class_specialization_subjects table)
        var classSpecializationSemesterMap = classSpecializationRecords
            .ToDictionary(css => css.SubjectId, css => css.Semester);

        _logger.LogInformation("Mapped {Count} class specialization subjects to semesters", classSpecializationSemesterMap.Count);

        // Fetch full Subject entities for class specialization
        var allSubjects = (await _subjectRepository.GetAllAsync(cancellationToken)).ToList();
        var classSpecializationSubjects = allSubjects
            .Where(s => classSpecializationSubjectIds.Contains(s.Id))
            .ToList();

        _logger.LogInformation("Retrieved {Count} class specialization subjects with full details", classSpecializationSubjects.Count);

        // ⭐ CRITICAL FIX: Combine both sources and OVERRIDE semester for class subjects
        var idealSubjects = routeSubjects
            .Concat(classSpecializationSubjects)
            .DistinctBy(s => s.Id)
            .ToList();

        // ⭐ NEW: For class specialization subjects, use their semester from the CSS table
        foreach (var subject in idealSubjects.Where(s => classSpecializationSubjectIds.Contains(s.Id)))
        {
            if (classSpecializationSemesterMap.TryGetValue(subject.Id, out var cssEsemester))
            {
                // Override the subject's semester with the class specialization semester
                subject.Semester = cssEsemester;
                _logger.LogInformation("Overriding semester for class subject {SubjectCode}: {Semester}", subject.SubjectCode, cssEsemester);
            }
        }

        _logger.LogInformation("Combined ideal subject list: {Count} total subjects (Program + Class Specialization)", idealSubjects.Count);

        // Filter out excluded subjects
        var filteredIdealSubjects = idealSubjects
            .Where(s => !IsExcludedSubject(s))
            .ToList();

        _logger.LogInformation("After filtering excluded subjects: {Count} subjects remain", filteredIdealSubjects.Count);

        // Get IDs from filtered list
        var idealSubjectIds = filteredIdealSubjects.Select(s => s.Id).ToHashSet();

        // Get the "current" state from the user's learning path
        var currentChapters = (await _questChapterRepository.FindAsync(c => c.LearningPathId == learningPath.Id, cancellationToken)).ToList();
        var currentQuests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.QuestChapterId.HasValue && currentChapters.Select(c => c.Id).Contains(q.QuestChapterId.Value))
            .ToList();
        var currentSubjectIdsWithQuests = currentQuests.Where(q => q.SubjectId.HasValue).Select(q => q.SubjectId!.Value).ToHashSet();

        // Identify quests to archive (subjects that are no longer in the curriculum)
        var questsToArchive = currentQuests
            .Where(q => q.SubjectId.HasValue && !idealSubjectIds.Contains(q.SubjectId.Value) && q.IsActive)
            .ToList();

        if (questsToArchive.Any())
        {
            _logger.LogInformation("Archiving {Count} quests for subjects no longer in the user's curriculum.", questsToArchive.Count);
            foreach (var quest in questsToArchive)
            {
                quest.IsActive = false; // Soft delete
                await _questRepository.UpdateAsync(quest, cancellationToken);
            }
        }

        // ⭐ FIXED: Now includes subjects with semesters from class_specialization_subjects
        var idealSemesterMap = filteredIdealSubjects
            .Where(subject => subject.Semester.HasValue)
            .GroupBy(subject => subject.Semester!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );

        _logger.LogInformation("Created semester map with {Count} semesters", idealSemesterMap.Count);

        foreach (var kvp in idealSemesterMap.OrderBy(x => x.Key))
        {
            var semesterNumber = kvp.Key;
            var subjectsInSemester = kvp.Value;

            _logger.LogInformation("Processing Semester {SemesterNumber} with {SubjectCount} subjects", semesterNumber, subjectsInSemester.Count);

            // Find or create the chapter for this semester
            var chapterTitle = $"Semester {semesterNumber}";
            var chapter = currentChapters.FirstOrDefault(c => c.Title == chapterTitle);
            if (chapter == null)
            {
                chapter = new QuestChapter
                {
                    LearningPathId = learningPath.Id,
                    Title = chapterTitle,
                    Sequence = semesterNumber,
                    Status = PathProgressStatus.NotStarted
                };
                chapter = await _questChapterRepository.AddAsync(chapter, cancellationToken);
                _logger.LogInformation("Created new QuestChapter: {Title}", chapter.Title);
            }

            // Get all student semester subjects for this user to determine recommendation reason
            var userSemesterSubjects = (await _studentSemesterSubjectRepository.FindAsync(
                ss => ss.AuthUserId == request.AuthUserId, cancellationToken)).ToList();

            // Identify new quests to create for this chapter
            int questSequence = 1;
            foreach (var subject in subjectsInSemester.OrderBy(s => s.SubjectCode))
            {
                // Determine recommendation status based on academic record
                var recommendationReason = DetermineRecommendationReason(userSemesterSubjects, subject.Id);
                // Rule: Only recommend if explicitly studying or failed. "Passed" or "Not Started" are not recommended.
                bool isRecommended = recommendationReason == "Studying" || recommendationReason == "Failed";

                if (!currentSubjectIdsWithQuests.Contains(subject.Id))
                {
                    // This subject is in the ideal curriculum but doesn't have a quest yet. Create one.
                    _logger.LogInformation("Creating new shell quest for Subject {SubjectCode} ({SubjectName}) - Semester {Semester} - with recommendation: {Reason} (IsRecommended: {IsRec})",
                        subject.SubjectCode, subject.SubjectName, semesterNumber, recommendationReason, isRecommended);

                    var newQuest = new Quest
                    {
                        Title = subject.SubjectCode + ": " + subject.SubjectName,
                        Description = subject.Description ?? $"Embark on the quest for {subject.SubjectName}.",
                        QuestType = QuestType.Practice,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        QuestChapterId = chapter.Id,
                        SubjectId = subject.Id,
                        Sequence = questSequence++,
                        Status = QuestStatus.NotStarted,
                        IsActive = true,
                        CreatedBy = userProfile.AuthUserId,
                        IsRecommended = isRecommended,
                        RecommendationReason = recommendationReason
                    };
                    await _questRepository.AddAsync(newQuest, cancellationToken);
                }
                else
                {
                    // Update existing quest with new sequence and recommendation status
                    var existingQuest = currentQuests.First(q => q.SubjectId == subject.Id);
                    bool needsUpdate = false;

                    // Update sequence if changed
                    if (existingQuest.Sequence != questSequence)
                    {
                        existingQuest.Sequence = questSequence;
                        needsUpdate = true;
                    }

                    // Reactivate if archived
                    if (!existingQuest.IsActive)
                    {
                        existingQuest.IsActive = true;
                        needsUpdate = true;
                    }

                    // Update recommendation status if changed (e.g. status changed from Studying to Passed)
                    if (existingQuest.IsRecommended != isRecommended)
                    {
                        existingQuest.IsRecommended = isRecommended;
                        needsUpdate = true;
                    }

                    if (existingQuest.RecommendationReason != recommendationReason)
                    {
                        existingQuest.RecommendationReason = recommendationReason;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        await _questRepository.UpdateAsync(existingQuest, cancellationToken);
                    }

                    questSequence++;
                }
            }
        }

        _logger.LogInformation("Successfully reconciled QuestLine structure {LearningPathId} with {SubjectCount} ideal subjects",
            learningPath.Id, idealSubjects.Count);

        return new GenerateQuestLineResponse
        {
            LearningPathId = learningPath.Id,
        };
    }

    private static bool IsExcludedSubject(Subject subject)
    {
        // Check by code
        if (ExcludedSubjectCodes.Contains(subject.SubjectCode))
            return true;

        // Check by name (catches variants)
        if (!string.IsNullOrEmpty(subject.SubjectName))
        {
            var nameLower = subject.SubjectName.ToLowerInvariant();
            if (nameLower.Contains("musical instrument")
                || nameLower.Contains("orientation")
                || nameLower.Contains("vovinam"))
                return true;
        }
        return false;
    }

    // Helper method to determine recommendation reason
    private string DetermineRecommendationReason(List<StudentSemesterSubject> userSemesterSubjects, Guid subjectId)
    {
        var studentSubject = userSemesterSubjects.FirstOrDefault(ss => ss.SubjectId == subjectId);

        if (studentSubject == null)
        {
            return "Recommended"; // Not Started / Suggested
        }

        return studentSubject.Status switch
        {
            SubjectEnrollmentStatus.Passed => "Passed",
            SubjectEnrollmentStatus.Studying => "Studying",
            SubjectEnrollmentStatus.NotPassed => "Failed",
            SubjectEnrollmentStatus.NotStarted => "Recommended",
            _ => "Recommended"
        };
    }
}