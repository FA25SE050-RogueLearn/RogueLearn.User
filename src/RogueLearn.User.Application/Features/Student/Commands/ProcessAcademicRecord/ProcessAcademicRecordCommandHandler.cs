// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using HtmlAgilityPack;
using System.Security.Cryptography;
using System.Text;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class FapRecordData
{
    // MODIFIED: Changed from double to double? to allow for null values from the AI.
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
        ICurriculumImportStorage storage)
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
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[START] Processing academic record for user {AuthUserId}", request.AuthUserId);

        _logger.LogInformation("Step 1: Pre-processing HTML to extract clean text.");
        string cleanTextForAi = PreprocessFapHtml(request.FapHtmlContent);
        if (string.IsNullOrWhiteSpace(cleanTextForAi))
        {
            _logger.LogWarning("HTML pre-processing failed. No grade report table found.");
            throw new BadRequestException("Could not find a valid grade report table in the provided HTML.");
        }
        _logger.LogInformation("HTML pre-processing successful.");

        var textHash = ComputeSha256Hash(cleanTextForAi);
        string? extractedJson = null;

        _logger.LogInformation("Step 2: Checking cache for existing extraction with hash {TextHash}.", textHash);
        try
        {
            extractedJson = await _storage.TryGetByHashJsonAsync("curriculum-imports", textHash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached academic record by hash {Hash}. Proceeding with AI extraction.", textHash);
        }

        if (!string.IsNullOrEmpty(extractedJson))
        {
            _logger.LogInformation("Cache HIT for academic record hash {TextHash}. Skipping AI extraction.", textHash);
        }
        else
        {
            _logger.LogInformation("Cache MISS for academic record hash {TextHash}. Calling FAP extraction plugin.", textHash);
            extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanTextForAi, cancellationToken);
            if (string.IsNullOrWhiteSpace(extractedJson))
            {
                _logger.LogError("AI extraction failed to return valid JSON for the academic record.");
                throw new InvalidOperationException("AI extraction failed to return valid JSON for the academic record.");
            }
            _logger.LogInformation("AI extraction successful.");

            await _storage.SaveLatestAsync(
                "curriculum-imports",
                $"user-academic-records/{request.AuthUserId}",
                "latest",
                extractedJson,
                cleanTextForAi,
                textHash,
                cancellationToken);
            _logger.LogInformation("Saved new academic record extraction to cache with hash {TextHash}", textHash);
        }

        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (fapData == null)
        {
            _logger.LogError("Failed to deserialize the extracted academic JSON.");
            throw new BadRequestException("Failed to deserialize the extracted academic data.");
        }
        _logger.LogInformation("Successfully deserialized academic data with {SubjectCount} subjects.", fapData.Subjects.Count);

        _logger.LogInformation("Step 3: Synchronizing database records.");
        var enrollment = await _enrollmentRepository.FirstOrDefaultAsync(e => e.AuthUserId == request.AuthUserId && e.CurriculumVersionId == request.CurriculumVersionId, cancellationToken);

        if (enrollment == null)
        {
            _logger.LogInformation("No existing enrollment found for user {AuthUserId}. Creating new one.", request.AuthUserId);
            var newEnrollment = new StudentEnrollment
            {
                AuthUserId = request.AuthUserId,
                CurriculumVersionId = request.CurriculumVersionId,
                EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = Domain.Enums.EnrollmentStatus.Active
            };
            enrollment = await _enrollmentRepository.AddAsync(newEnrollment, cancellationToken);
            _logger.LogInformation("Created new enrollment with ID {EnrollmentId}", enrollment.Id);
        }
        else
        {
            _logger.LogInformation("Found existing enrollment with ID {EnrollmentId}", enrollment.Id);
        }

        foreach (var subjectRecord in fapData.Subjects)
        {
            var subject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == subjectRecord.SubjectCode, cancellationToken);
            if (subject == null)
            {
                _logger.LogWarning("Subject with code {SubjectCode} from transcript not found in database. Skipping.", subjectRecord.SubjectCode);
                continue;
            }

            var semesterSubject = await _semesterSubjectRepository.FirstOrDefaultAsync(ss => ss.EnrollmentId == enrollment.Id && ss.SubjectId == subject.Id, cancellationToken);
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
                _logger.LogInformation("Created new student semester subject record for {SubjectCode}", subject.SubjectCode);
            }
            else
            {
                semesterSubject.Status = parsedStatus;
                semesterSubject.Grade = subjectRecord.Mark?.ToString("F1");
                await _semesterSubjectRepository.UpdateAsync(semesterSubject, cancellationToken);
                _logger.LogInformation("Updated existing student semester subject record for {SubjectCode}", subject.SubjectCode);
            }
        }

        var learningPath = await _learningPathRepository.FirstOrDefaultAsync(lp => lp.CreatedBy == request.AuthUserId && lp.CurriculumVersionId == request.CurriculumVersionId, cancellationToken);
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
            _logger.LogInformation("Created new Learning Path with ID {LearningPathId}", learningPath.Id);
        }
        else
        {
            _logger.LogInformation("Found existing Learning Path with ID {LearningPathId}", learningPath.Id);
        }

        var structures = (await _curriculumStructureRepository.FindAsync(cs => cs.CurriculumVersionId == request.CurriculumVersionId, cancellationToken)).ToList();
        _logger.LogInformation("Found {StructureCount} subjects in the official curriculum structure.", structures.Count);

        int questsCreated = 0;
        int questsUpdated = 0;
        var semesters = structures.GroupBy(s => s.Semester).OrderBy(g => g.Key);

        foreach (var semesterGroup in semesters)
        {
            var chapter = await _questChapterRepository.FirstOrDefaultAsync(qc => qc.LearningPathId == learningPath.Id && qc.Sequence == semesterGroup.Key, cancellationToken);
            if (chapter == null)
            {
                chapter = new QuestChapter
                {
                    LearningPathId = learningPath.Id,
                    Title = $"Semester {semesterGroup.Key}",
                    Sequence = semesterGroup.Key,
                    Status = PathProgressStatus.NotStarted
                };
                await _questChapterRepository.AddAsync(chapter, cancellationToken);
                _logger.LogInformation("Created new Quest Chapter for Semester {Semester}", semesterGroup.Key);
            }

            foreach (var structure in semesterGroup)
            {
                var subject = await _subjectRepository.GetByIdAsync(structure.SubjectId, cancellationToken);
                if (subject == null) continue;

                var quest = await _questRepository.FirstOrDefaultAsync(q => q.SubjectId == subject.Id && q.CreatedBy == request.AuthUserId, cancellationToken);
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
                    _logger.LogInformation("Created new Quest '{QuestTitle}' for Subject {SubjectCode}", quest.Title, subject.SubjectCode);

                    var learningPathQuest = new LearningPathQuest
                    {
                        LearningPathId = learningPath.Id,
                        QuestId = quest.Id,
                        DifficultyLevel = quest.DifficultyLevel,
                        SequenceOrder = questsCreated + questsUpdated,
                        IsMandatory = structure.IsMandatory,
                    };
                    await _learningPathQuestRepository.AddAsync(learningPathQuest, cancellationToken);
                }

                var userRecord = fapData.Subjects.FirstOrDefault(s => s.SubjectCode == subject.SubjectCode);
                var questStatus = QuestStatus.NotStarted;
                if (userRecord != null)
                {
                    questStatus = MapFapStatusToQuestStatus(userRecord.Status);
                }

                var progress = await _questProgressRepository.FirstOrDefaultAsync(p => p.AuthUserId == request.AuthUserId && p.QuestId == quest.Id, cancellationToken);
                if (progress == null)
                {
                    progress = new UserQuestProgress
                    {
                        AuthUserId = request.AuthUserId,
                        QuestId = quest.Id,
                        Status = questStatus,
                        CompletedAt = questStatus == QuestStatus.Completed ? DateTimeOffset.UtcNow : null
                    };
                    await _questProgressRepository.AddAsync(progress, cancellationToken);
                    _logger.LogInformation("Created new UserQuestProgress for Quest '{QuestTitle}' with Status {Status}", quest.Title, questStatus);
                }
                else
                {
                    progress.Status = questStatus;
                    progress.CompletedAt = questStatus == QuestStatus.Completed ? DateTimeOffset.UtcNow : null;
                    progress.LastUpdatedAt = DateTimeOffset.UtcNow;
                    await _questProgressRepository.UpdateAsync(progress, cancellationToken);
                    questsUpdated++;
                    _logger.LogInformation("Updated UserQuestProgress for Quest '{QuestTitle}' to Status {Status}", quest.Title, questStatus);
                }
            }
        }

        _logger.LogInformation("[END] Finished processing academic record. Quests Created: {QuestsCreated}, Quests Updated: {QuestsUpdated}", questsCreated, questsUpdated);
        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed and questline synchronized successfully.",
            LearningPathId = learningPath.Id,
            SubjectsProcessed = fapData.Subjects.Count,
            QuestsGenerated = questsCreated,
            // MODIFIED: Safely access the nullable Gpa. Use 0.0 as a default if it's null.
            CalculatedGpa = fapData.Gpa ?? 0.0
        };
    }

    private string PreprocessFapHtml(string rawHtml)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(rawHtml);

        var gradeTableNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='Grid']//table[contains(@class, 'table-hover')][1]");

        if (gradeTableNode == null)
        {
            _logger.LogWarning("Could not find the grade report table using XPath within the provided FAP HTML.");
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