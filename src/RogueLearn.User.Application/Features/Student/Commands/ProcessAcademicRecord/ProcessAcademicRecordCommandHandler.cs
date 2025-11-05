// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommandHandler.cs
using HtmlAgilityPack;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class FapRecordData
{
    public double? Gpa { get; set; }
    public List<FapSubjectData> Subjects { get; set; } = new();
}

public class FapSubjectData
{
    public string SubjectCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? Mark { get; set; }
}

public class ProcessAcademicRecordCommandHandler : IRequestHandler<ProcessAcademicRecordCommand, ProcessAcademicRecordResponse>
{
    private readonly IFapExtractionPlugin _fapPlugin;
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _questProgressRepository;
    private readonly ILearningPathQuestRepository _learningPathQuestRepository;
    private readonly ILogger<ProcessAcademicRecordCommandHandler> _logger;
    private readonly ICurriculumImportStorage _storage;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;

    public ProcessAcademicRecordCommandHandler(
        IFapExtractionPlugin fapPlugin,
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository questProgressRepository,
        ILearningPathQuestRepository learningPathQuestRepository,
        ILogger<ProcessAcademicRecordCommandHandler> logger,
        ICurriculumImportStorage storage,
        ICurriculumVersionRepository curriculumVersionRepository)
    {
        _fapPlugin = fapPlugin;
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _questProgressRepository = questProgressRepository;
        _learningPathQuestRepository = learningPathQuestRepository;
        _logger = logger;
        _storage = storage;
        _curriculumVersionRepository = curriculumVersionRepository;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[START] Processing academic record for user {AuthUserId}", request.AuthUserId);

        // STEP 1: Validate curriculum version
        var curriculumVersion = await _curriculumVersionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (curriculumVersion == null)
        {
            _logger.LogWarning("Invalid CurriculumVersionId: {CurriculumVersionId}", request.CurriculumVersionId);
            throw new NotFoundException(nameof(CurriculumVersion), request.CurriculumVersionId);
        }

        // STEP 2: Pre-process HTML
        _logger.LogInformation("Step 1: Pre-processing HTML to extract clean text.");
        string cleanTextForAi = PreprocessFapHtml(request.FapHtmlContent);
        if (string.IsNullOrWhiteSpace(cleanTextForAi))
        {
            _logger.LogWarning("HTML pre-processing failed. No grade report table found.");
            throw new BadRequestException("Could not find a valid grade report table in the provided HTML.");
        }

        // STEP 3: Check cache or call AI
        var textHash = ComputeSha256Hash(cleanTextForAi);
        string? extractedJson = null;

        _logger.LogInformation("Step 2: Checking cache for hash {TextHash}.", textHash);
        try
        {
            extractedJson = await _storage.TryGetByHashJsonAsync("curriculum-imports", textHash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached record. Proceeding with AI extraction.");
        }

        if (string.IsNullOrEmpty(extractedJson))
        {
            _logger.LogInformation("Cache MISS. Calling FAP extraction plugin.");
            extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanTextForAi, cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedJson))
            {
                _logger.LogError("AI extraction failed to return valid JSON.");
                throw new InvalidOperationException("AI extraction failed to return valid JSON for the academic record.");
            }

            await _storage.SaveLatestAsync(
                "curriculum-imports",
                $"user-academic-records/{request.AuthUserId}",
                "latest",
                extractedJson,
                cleanTextForAi,
                textHash,
                cancellationToken);

            _logger.LogInformation("Saved extraction to cache with hash {TextHash}", textHash);
        }
        else
        {
            _logger.LogInformation("Cache HIT for hash {TextHash}.", textHash);
        }

        // STEP 4: Deserialize FAP data
        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (fapData == null)
        {
            _logger.LogError("Failed to deserialize the extracted academic JSON.");
            throw new BadRequestException("Failed to deserialize the extracted academic data.");
        }

        _logger.LogInformation("Successfully deserialized {SubjectCount} subjects.", fapData.Subjects.Count);

        // STEP 5: Create or get enrollment
        _logger.LogInformation("Step 3: Synchronizing database records.");
        var enrollment = await _enrollmentRepository.FirstOrDefaultAsync(
            e => e.AuthUserId == request.AuthUserId && e.CurriculumVersionId == request.CurriculumVersionId,
            cancellationToken);

        if (enrollment == null)
        {
            _logger.LogInformation("Creating new enrollment for user {AuthUserId}.", request.AuthUserId);
            var newEnrollment = new StudentEnrollment
            {
                AuthUserId = request.AuthUserId,
                CurriculumVersionId = request.CurriculumVersionId,
                EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = EnrollmentStatus.Active
            };
            enrollment = await _enrollmentRepository.AddAsync(newEnrollment, cancellationToken);
            _logger.LogInformation("Created enrollment with ID {EnrollmentId}", enrollment.Id);
        }

        // STEP 6: Sync subjects
        var allDbSubjects = (await _subjectRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.SubjectCode);
        var missingSubjectCodes = fapData.Subjects
            .Select(s => s.SubjectCode)
            .Where(sc => !allDbSubjects.ContainsKey(sc))
            .Distinct()
            .ToList();

        if (missingSubjectCodes.Any())
        {
            _logger.LogWarning("Missing {Count} subjects in database: {MissingSubjects}",
                missingSubjectCodes.Count, string.Join(", ", missingSubjectCodes));
        }

        foreach (var subjectRecord in fapData.Subjects)
        {
            if (!allDbSubjects.TryGetValue(subjectRecord.SubjectCode, out var subject))
            {
                _logger.LogWarning("Subject {SubjectCode} not found in database. Skipping.", subjectRecord.SubjectCode);
                continue;
            }

            var semesterSubject = await _semesterSubjectRepository.FirstOrDefaultAsync(
                ss => ss.EnrollmentId == enrollment.Id && ss.SubjectId == subject.Id,
                cancellationToken);

            var parsedStatus = MapFapStatusToEnum(subjectRecord.Status);

            if (semesterSubject == null)
            {
                semesterSubject = new StudentSemesterSubject
                {
                    EnrollmentId = enrollment.Id,
                    SubjectId = subject.Id,
                    AcademicYear = "2024-2025",
                    Semester = 1,
                    Status = parsedStatus,
                    Grade = subjectRecord.Mark?.ToString("F1")
                };
                await _semesterSubjectRepository.AddAsync(semesterSubject, cancellationToken);
                _logger.LogDebug("Created semester subject record for {SubjectCode}", subject.SubjectCode);
            }
            else
            {
                semesterSubject.Status = parsedStatus;
                semesterSubject.Grade = subjectRecord.Mark?.ToString("F1");
                await _semesterSubjectRepository.UpdateAsync(semesterSubject, cancellationToken);
                _logger.LogDebug("Updated semester subject record for {SubjectCode}", subject.SubjectCode);
            }
        }

        // STEP 7: Create or get learning path
        var learningPath = await _learningPathRepository.FirstOrDefaultAsync(
            lp => lp.CreatedBy == request.AuthUserId && lp.CurriculumVersionId == request.CurriculumVersionId,
            cancellationToken);

        if (learningPath == null)
        {
            learningPath = new LearningPath
            {
                Name = $"Main Quest for Curriculum {request.CurriculumVersionId}",
                Description = "Your personalized learning journey based on your FPT University curriculum.",
                PathType = PathType.Course,
                CurriculumVersionId = request.CurriculumVersionId,
                IsPublished = true,
                CreatedBy = request.AuthUserId
            };
            learningPath = await _learningPathRepository.AddAsync(learningPath, cancellationToken);
            _logger.LogInformation("Created Learning Path with ID {LearningPathId}", learningPath.Id);
        }

        // STEP 8: Sync quests and chapters
        var structures = (await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == request.CurriculumVersionId,
            cancellationToken)).ToList();

        _logger.LogInformation("Found {StructureCount} subjects in curriculum structure.", structures.Count);

        int questsCreated = 0;
        int questsUpdated = 0;
        var semesters = structures.GroupBy(s => s.Semester).OrderBy(g => g.Key);

        var existingLearningPathQuests = (await _learningPathQuestRepository.FindAsync(
            lpq => lpq.LearningPathId == learningPath.Id,
            cancellationToken)).ToList();

        int nextSequenceOrder = existingLearningPathQuests.Any()
            ? existingLearningPathQuests.Max(lpq => lpq.SequenceOrder) + 1
            : 1;

        var fapPassedSubjects = fapData.Subjects
            .Where(s => "Passed".Equals(s.Status, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.SubjectCode)
            .ToHashSet();

        // Determine first in-progress semester
        int firstInProgressSemester = -1;
        foreach (var semesterGroup in semesters)
        {
            bool allPassedInSemester = semesterGroup.All(structure =>
            {
                var subjectEntity = allDbSubjects.Values.FirstOrDefault(s => s.Id == structure.SubjectId);
                return subjectEntity != null && fapPassedSubjects.Contains(subjectEntity.SubjectCode);
            });

            if (!allPassedInSemester)
            {
                firstInProgressSemester = semesterGroup.Key;
                break;
            }
        }

        if (firstInProgressSemester == -1 && semesters.Any())
        {
            firstInProgressSemester = semesters.Last().Key;
        }

        // Create chapters and quests
        foreach (var semesterGroup in semesters)
        {
            var chapter = await _questChapterRepository.FirstOrDefaultAsync(
                qc => qc.LearningPathId == learningPath.Id && qc.Sequence == semesterGroup.Key,
                cancellationToken);

            if (chapter == null)
            {
                chapter = new QuestChapter
                {
                    LearningPathId = learningPath.Id,
                    Title = $"Semester {semesterGroup.Key}",
                    Sequence = semesterGroup.Key,
                    Status = (semesterGroup.Key == firstInProgressSemester)
                        ? PathProgressStatus.InProgress
                        : PathProgressStatus.NotStarted
                };
                await _questChapterRepository.AddAsync(chapter, cancellationToken);
                _logger.LogDebug("Created Quest Chapter for Semester {Semester}", semesterGroup.Key);
            }

            foreach (var structure in semesterGroup)
            {
                var subject = allDbSubjects.Values.FirstOrDefault(s => s.Id == structure.SubjectId);
                if (subject == null) continue;

                var quest = await _questRepository.FirstOrDefaultAsync(
                    q => q.SubjectId == subject.Id && q.CreatedBy == request.AuthUserId,
                    cancellationToken);

                if (quest == null)
                {
                    quest = new Quest
                    {
                        Title = subject.SubjectName,
                        Description = subject.Description ?? $"Complete the objectives for {subject.SubjectCode}.",
                        QuestType = QuestType.Practice,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ExperiencePointsReward = subject.Credits * 50,
                        SubjectId = subject.Id,
                        IsActive = true,
                        CreatedBy = request.AuthUserId
                    };
                    quest = await _questRepository.AddAsync(quest, cancellationToken);
                    questsCreated++;

                    var existingLink = existingLearningPathQuests.FirstOrDefault(lpq => lpq.QuestId == quest.Id);
                    if (existingLink == null)
                    {
                        var learningPathQuest = new LearningPathQuest
                        {
                            LearningPathId = learningPath.Id,
                            QuestId = quest.Id,
                            DifficultyLevel = quest.DifficultyLevel,
                            SequenceOrder = nextSequenceOrder++,
                            IsMandatory = structure.IsMandatory,
                        };
                        await _learningPathQuestRepository.AddAsync(learningPathQuest, cancellationToken);
                    }
                }

                // Update quest progress
                var userRecord = fapData.Subjects.FirstOrDefault(s => s.SubjectCode == subject.SubjectCode);
                var questStatus = QuestStatus.NotStarted;
                if (userRecord != null)
                {
                    questStatus = MapFapStatusToQuestStatus(userRecord.Status);
                }

                var progress = await _questProgressRepository.FirstOrDefaultAsync(
                    p => p.AuthUserId == request.AuthUserId && p.QuestId == quest.Id,
                    cancellationToken);

                if (progress == null && userRecord != null)
                {
                    progress = new UserQuestProgress
                    {
                        AuthUserId = request.AuthUserId,
                        QuestId = quest.Id,
                        Status = questStatus,
                        CompletedAt = questStatus == QuestStatus.Completed ? DateTimeOffset.UtcNow : null
                    };
                    await _questProgressRepository.AddAsync(progress, cancellationToken);
                }
                else if (progress != null && userRecord != null)
                {
                    progress.Status = questStatus;
                    progress.CompletedAt = questStatus == QuestStatus.Completed ? DateTimeOffset.UtcNow : null;
                    progress.LastUpdatedAt = DateTimeOffset.UtcNow;
                    await _questProgressRepository.UpdateAsync(progress, cancellationToken);
                    questsUpdated++;
                }
            }
        }

        // REMOVED: InitializeUserSkillsForCurriculum call
        // Skills will be initialized via separate endpoint call

        _logger.LogInformation(
            "[END] Academic record processed. Quests Created: {QuestsCreated}, Updated: {QuestsUpdated}",
            questsCreated, questsUpdated);

        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed successfully. Call /initialize-skills to set up your skill tree.",
            LearningPathId = learningPath.Id,
            SubjectsProcessed = fapData.Subjects.Count,
            QuestsGenerated = questsCreated,
            CalculatedGpa = fapData.Gpa ?? 0.0
        };
    }

    private string PreprocessFapHtml(string rawHtml)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(rawHtml);

        var gradeTableNode = htmlDoc.DocumentNode.SelectSingleNode(
            "//div[@id='Grid']//table[contains(@class, 'table-hover')][1]");

        if (gradeTableNode == null)
        {
            _logger.LogWarning("Could not find grade report table in FAP HTML.");
            return string.Empty;
        }

        return gradeTableNode.InnerText;
    }

    private string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private SubjectEnrollmentStatus MapFapStatusToEnum(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "passed" => SubjectEnrollmentStatus.Completed,
            "failed" => SubjectEnrollmentStatus.Failed,
            "studying" => SubjectEnrollmentStatus.Enrolled,
            _ => SubjectEnrollmentStatus.Enrolled
        };
    }

    private QuestStatus MapFapStatusToQuestStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "passed" => QuestStatus.Completed,
            "failed" => QuestStatus.InProgress,
            "studying" => QuestStatus.InProgress,
            _ => QuestStatus.NotStarted
        };
    }
}