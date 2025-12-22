using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Student.Commands.UpdateSingleSubjectGrade;

public class UpdateSingleSubjectGradeCommandHandler : IRequestHandler<UpdateSingleSubjectGradeCommand, UpdateSingleSubjectGradeResponse>
{
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IGradeExperienceCalculator _gradeExperienceCalculator;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateSingleSubjectGradeCommandHandler> _logger;

    public UpdateSingleSubjectGradeCommandHandler(
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IGradeExperienceCalculator gradeExperienceCalculator,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IQuestDifficultyResolver difficultyResolver,
        IUserQuestStepProgressRepository stepProgressRepository,
        IMediator mediator,
        ILogger<UpdateSingleSubjectGradeCommandHandler> logger)
    {
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _gradeExperienceCalculator = gradeExperienceCalculator;
        _questRepository = questRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _difficultyResolver = difficultyResolver;
        _stepProgressRepository = stepProgressRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<UpdateSingleSubjectGradeResponse> Handle(UpdateSingleSubjectGradeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating single subject grade for User {UserId}, Subject {SubjectId}", request.AuthUserId, request.SubjectId);

        // 1. Validate Subject Exists
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            throw new NotFoundException("Subject", request.SubjectId);
        }

        // 2. Validate Grade Range
        if (request.Grade < 0 || request.Grade > 10)
        {
            throw new BadRequestException("Grade must be between 0 and 10.");
        }

        // 3. Find or Create Student Semester Subject Record
        var existingRecords = await _semesterSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId && ss.SubjectId == request.SubjectId,
            cancellationToken);

        var record = existingRecords.FirstOrDefault();
        bool isNewRecord = false;

        if (record == null)
        {
            isNewRecord = true;
            record = new StudentSemesterSubject
            {
                Id = Guid.NewGuid(),
                AuthUserId = request.AuthUserId,
                SubjectId = request.SubjectId,
                AcademicYear = request.AcademicYear,
                EnrolledAt = DateTimeOffset.UtcNow
            };
        }

        // 4. Update Properties
        record.Grade = request.Grade.ToString("F1");
        record.Status = request.Status;
        record.CreditsEarned = request.Status == SubjectEnrollmentStatus.Passed ? subject.Credits : 0;

        if (!string.IsNullOrWhiteSpace(request.AcademicYear))
        {
            record.AcademicYear = request.AcademicYear;
        }
        else if (isNewRecord)
        {
            record.AcademicYear = $"{DateTime.UtcNow.Year}";
        }

        if (request.Status == SubjectEnrollmentStatus.Passed)
        {
            record.CompletedAt = DateTimeOffset.UtcNow;
        }

        // 5. Persist Changes
        if (isNewRecord)
        {
            await _semesterSubjectRepository.AddAsync(record, cancellationToken);
        }
        else
        {
            await _semesterSubjectRepository.UpdateAsync(record, cancellationToken);
        }

        _logger.LogInformation("Successfully updated grade record for {SubjectCode}. New Grade: {Grade}, Status: {Status}",
            subject.SubjectCode, record.Grade, record.Status);

        // 6. Award XP if Passed
        XpAwardSummary? xpSummary = null;
        if (record.Status == SubjectEnrollmentStatus.Passed)
        {
            xpSummary = await AwardXpForSubjectAsync(request.AuthUserId, subject, request.Grade, cancellationToken);
        }

        // 7. OPTIMIZED: Update Difficulty Directly (Skipping full AI analysis)
        // Find the Master Quest associated with this subject
        var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);

        if (masterQuest != null)
        {
            // Find user's existing attempt
            var attempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == masterQuest.Id,
                cancellationToken);

            // Calculate new difficulty based purely on the new grade
            // NOTE: We pass -1.0 for proficiency to skip prerequisite checks, assuming the grade is the primary signal now
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(record, -1.0, subject);
            var newDifficulty = difficultyInfo.ExpectedDifficulty;

            if (attempt == null)
            {
                // Create new attempt with correct difficulty
                attempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = masterQuest.Id,
                    Status = QuestAttemptStatus.NotStarted,
                    AssignedDifficulty = newDifficulty,
                    Notes = difficultyInfo.DifficultyReason, // e.g. "Excellent score (9.0) - advanced content"
                    StartedAt = DateTimeOffset.UtcNow
                };
                await _userQuestAttemptRepository.AddAsync(attempt, cancellationToken);
                _logger.LogInformation("Created new quest attempt for {SubjectCode} with difficulty {Difficulty}", subject.SubjectCode, newDifficulty);
            }
            else
            {
                // Update existing attempt if difficulty changed
                if (attempt.AssignedDifficulty != newDifficulty)
                {
                    _logger.LogInformation("Adjusting difficulty for {SubjectCode} from {Old} to {New} based on grade update.",
                        subject.SubjectCode, attempt.AssignedDifficulty, newDifficulty);

                    // If they are downgrading or upgrading, reset step progress to align with new track
                    await _stepProgressRepository.DeleteByAttemptIdAsync(attempt.Id, cancellationToken);

                    attempt.AssignedDifficulty = newDifficulty;
                    attempt.Notes = $"Difficulty updated on {DateTime.UtcNow:yyyy-MM-dd}. Reason: {difficultyInfo.DifficultyReason}";

                    // Reset status to allow re-attempting on new track
                    if (attempt.Status == QuestAttemptStatus.Completed)
                    {
                        attempt.Status = QuestAttemptStatus.InProgress;
                    }
                    attempt.CurrentStepId = null; // Will be re-set when they click Start/Resume

                    await _userQuestAttemptRepository.UpdateAsync(attempt, cancellationToken);
                }
            }
        }

        return new UpdateSingleSubjectGradeResponse
        {
            SubjectId = subject.Id,
            SubjectCode = subject.SubjectCode,
            NewGrade = record.Grade,
            NewStatus = record.Status.ToString(),
            XpAwarded = xpSummary
        };
    }

    private async Task<XpAwardSummary> AwardXpForSubjectAsync(
        Guid authUserId,
        Subject subject,
        double grade,
        CancellationToken cancellationToken)
    {
        var summary = new XpAwardSummary();
        var mappings = await _subjectSkillMappingRepository.FindAsync(m => m.SubjectId == subject.Id, cancellationToken);
        if (!mappings.Any()) return summary;

        var semester = subject.Semester ?? 1;
        var tierInfo = _gradeExperienceCalculator.GetTierInfo(semester);

        foreach (var mapping in mappings)
        {
            var xpAmount = _gradeExperienceCalculator.CalculateXpAward(grade, semester, mapping.RelevanceWeight);
            var response = await _mediator.Send(new IngestXpEventCommand
            {
                AuthUserId = authUserId,
                SkillId = mapping.SkillId,
                Points = xpAmount,
                SourceService = "ManualGradeUpdate",
                SourceType = "GradeUpdate",
                SourceId = subject.Id,
                Reason = $"Manual grade update: {subject.SubjectCode} ({grade:F1}/10.0)"
            }, cancellationToken);

            if (response.Processed)
            {
                summary.TotalXp += xpAmount;
                summary.SkillsAffected++;
                summary.SkillAwards.Add(new SkillXpAward
                {
                    SkillId = mapping.SkillId,
                    SkillName = response.SkillName,
                    XpAwarded = xpAmount,
                    NewTotalXp = response.NewExperiencePoints,
                    NewLevel = response.NewLevel,
                    SourceSubjectCode = subject.SubjectCode,
                    Grade = grade.ToString("F1"),
                    TierDescription = tierInfo.Description
                });
            }
        }

        return summary;
    }
}