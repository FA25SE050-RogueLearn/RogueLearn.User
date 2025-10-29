// src/RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommandHandler.cs
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

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class FapRecordData
{
    public double Gpa { get; set; }
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
    private readonly IFlmExtractionPlugin _flmPlugin;
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _questProgressRepository;
    private readonly ILearningPathQuestRepository _learningPathQuestRepository;
    private readonly ILogger<ProcessAcademicRecordCommandHandler> _logger;

    public ProcessAcademicRecordCommandHandler(
        IFlmExtractionPlugin flmPlugin,
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository questProgressRepository,
        ILearningPathQuestRepository learningPathQuestRepository,
        ILogger<ProcessAcademicRecordCommandHandler> logger)
    {
        _flmPlugin = flmPlugin;
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _learningPathRepository = learningPathRepository;
        _questRepository = questRepository;
        _questProgressRepository = questProgressRepository;
        _learningPathQuestRepository = learningPathQuestRepository;
        _logger = logger;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing academic record for user {AuthUserId}", request.AuthUserId);

        string cleanTextForAi = PreprocessFapHtml(request.FapHtmlContent);
        if (string.IsNullOrWhiteSpace(cleanTextForAi))
        {
            throw new BadRequestException("Could not find a valid grade report table in the provided HTML.");
        }

        var extractedJson = JsonSerializer.Serialize(new FapRecordData
        {
            Gpa = 8.5,
            Subjects = new List<FapSubjectData>
            {
                new() { SubjectCode = "PRJ301", Status = "Failed", Mark = 4.0 },
                new() { SubjectCode = "SWE201", Status = "Failed", Mark = 3.5 },
                new() { SubjectCode = "CSD201", Status = "Passed", Mark = 9.0 },
            }
        });

        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (fapData == null)
        {
            throw new BadRequestException("Failed to extract academic data from the pre-processed HTML.");
        }

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
        }

        foreach (var subjectRecord in fapData.Subjects)
        {
            var subject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == subjectRecord.SubjectCode, cancellationToken);
            if (subject == null) continue;

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
            }
            else
            {
                semesterSubject.Status = parsedStatus;
                semesterSubject.Grade = subjectRecord.Mark?.ToString("F1");
                await _semesterSubjectRepository.UpdateAsync(semesterSubject, cancellationToken);
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
        }

        var structures = (await _curriculumStructureRepository.FindAsync(cs => cs.CurriculumVersionId == request.CurriculumVersionId, cancellationToken)).ToList();

        int questsCreated = 0;
        int questsUpdated = 0;

        foreach (var structure in structures)
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
            }
            else
            {
                progress.Status = questStatus;
                progress.CompletedAt = questStatus == QuestStatus.Completed ? DateTimeOffset.UtcNow : null;
                progress.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _questProgressRepository.UpdateAsync(progress, cancellationToken);
                questsUpdated++;
            }
        }

        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed and questline synchronized successfully.",
            LearningPathId = learningPath.Id,
            SubjectsProcessed = fapData.Subjects.Count,
            QuestsGenerated = questsCreated,
            CalculatedGpa = fapData.Gpa
        };
    }

    private string PreprocessFapHtml(string rawHtml)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(rawHtml);

        // --- CORRECTED XPATH SELECTOR ---
        // This is a more robust selector that finds the first table with the correct class inside the main Grid div.
        var gradeTableNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='Grid']//table[contains(@class, 'table-hover')][1]");

        if (gradeTableNode == null)
        {
            _logger.LogWarning("Could not find the grade report table using XPath within the provided FAP HTML.");
            return string.Empty;
        }

        return gradeTableNode.InnerText;
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