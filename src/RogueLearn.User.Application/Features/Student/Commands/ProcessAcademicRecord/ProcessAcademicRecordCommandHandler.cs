// src/RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models; // Assuming your AI models are here
using RogueLearn.User.Application.Plugins; // Assuming AI plugin is here
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

// This model represents the structured data we expect from the AI after it parses the FAP HTML
public class FapRecordData
{
    public double Gpa { get; set; }
    public List<FapSubjectData> Subjects { get; set; } = new();
}

public class FapSubjectData
{
    public string SubjectCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Passed", "Failed", "InProgress"
    public double? Mark { get; set; }
}

public class ProcessAcademicRecordCommandHandler : IRequestHandler<ProcessAcademicRecordCommand, ProcessAcademicRecordResponse>
{
    private readonly IFlmExtractionPlugin _flmPlugin; // We can reuse this or create a new one for FAP
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _questProgressRepository;
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
        _logger = logger;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing academic record for user {AuthUserId} and curriculum version {CurriculumVersionId}", request.AuthUserId, request.CurriculumVersionId);

        // 1. AI Extraction of Grades and Status from HTML
        // This would be a new function in your IFlmExtractionPlugin
        // var extractedJson = await _flmPlugin.ExtractFapRecordJsonAsync(request.FapHtmlContent, cancellationToken);
        // For now, we'll use mock data to simulate the AI's output.
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
            throw new BadRequestException("Failed to extract academic data from HTML.");
        }

        // 2. Persist the user's academic record
        var enrollment = await _enrollmentRepository.FirstOrDefaultAsync(e => e.AuthUserId == request.AuthUserId && e.CurriculumVersionId == request.CurriculumVersionId, cancellationToken);
        if (enrollment == null)
        {
            throw new NotFoundException("User is not enrolled in the specified curriculum version.");
        }

        foreach (var subjectRecord in fapData.Subjects)
        {
            var subject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == subjectRecord.SubjectCode, cancellationToken);
            if (subject == null) continue;

            // This part of logic needs to be more robust to find the correct academic year/semester
            var semesterSubject = new StudentSemesterSubject
            {
                EnrollmentId = enrollment.Id,
                SubjectId = subject.Id,
                AcademicYear = "2024-2025", // Placeholder
                Semester = 1, // Placeholder
                Status = Enum.Parse<SubjectEnrollmentStatus>(subjectRecord.Status, true),
                Grade = subjectRecord.Mark?.ToString("F1")
            };
            // This needs an upsert logic in a real scenario
            await _semesterSubjectRepository.AddAsync(semesterSubject, cancellationToken);
        }

        // 3. Generate the QuestLine (or update it)
        // This is a simplified version of the logic from the previous step, now adapted for this context.
        var learningPath = await _learningPathRepository.FirstOrDefaultAsync(lp => lp.CreatedBy == request.AuthUserId && lp.CurriculumVersionId == request.CurriculumVersionId, cancellationToken);
        if (learningPath == null)
        {
            // Create a new learning path if one doesn't exist
            learningPath = new LearningPath
            {
                Name = $"Main Quest for Curriculum {request.CurriculumVersionId}",
                Description = "Your personalized learning journey based on your FPT University curriculum.",
                PathType = PathType.Course,
                CurriculumVersionId = request.CurriculumVersionId,
                IsPublished = true,
                CreatedBy = request.AuthUserId
            };
            await _learningPathRepository.AddAsync(learningPath, cancellationToken);
        }

        var structures = (await _curriculumStructureRepository.FindAsync(cs => cs.CurriculumVersionId == request.CurriculumVersionId, cancellationToken)).ToList();

        foreach (var structure in structures)
        {
            var subject = await _subjectRepository.GetByIdAsync(structure.SubjectId, cancellationToken);
            if (subject == null) continue;

            // Check if a quest for this subject already exists in the learning path
            // This requires a new repository method or more complex logic
            var quest = new Quest
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
            await _questRepository.AddAsync(quest, cancellationToken);

            var userRecord = fapData.Subjects.FirstOrDefault(s => s.SubjectCode == subject.SubjectCode);
            var questStatus = QuestStatus.NotStarted;
            if (userRecord != null)
            {
                questStatus = userRecord.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase)
                    ? QuestStatus.Completed
                    : QuestStatus.InProgress; // Or "Failed" if you add that status
            }

            var progress = new UserQuestProgress
            {
                AuthUserId = request.AuthUserId,
                QuestId = quest.Id,
                Status = questStatus,
                CompletedAt = questStatus == QuestStatus.Completed ? DateTimeOffset.UtcNow : null
            };
            await _questProgressRepository.AddAsync(progress, cancellationToken);
        }

        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed and questline generated successfully.",
            LearningPathId = learningPath.Id,
            SubjectsProcessed = fapData.Subjects.Count,
            QuestsGenerated = structures.Count,
            CalculatedGpa = fapData.Gpa
        };
    }
}